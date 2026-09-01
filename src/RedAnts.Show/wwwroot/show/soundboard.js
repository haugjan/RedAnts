// Browser-Interop fürs Soundboard. Alle UI-/Ablauflogik liegt in Blazor/C#;
// hier stehen nur die unvermeidbaren Browser-APIs: HTML-Audio (lokale Effekte)
// und das Spotify Web Playback SDK samt PKCE-Login.
(function () {
  const board = {};
  let dotnet = null;
  let assetBase = '';
  let appBase = '/show/';

  // ---------- lokale Audio-Engine (ein einziges Audio-Element) ----------
  // iOS/Safari blockiert play(), wenn es nicht direkt in einer Nutzergeste steht.
  // Daher wird playLocal aus dem echten Klick-Event heraus aufgerufen (siehe
  // Delegations-Handler unten), nicht über den Blazor-Server-Roundtrip.
  const SILENT_WAV = 'data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEARKwAAIhYAQACABAAZGF0YQAAAAA=';
  let volume = 0.9;
  let mediaEl = null;
  let activeId = null;
  let activeTimer = null;
  let mediaUnlocked = false;

  // Einmalige Audio-Freischaltung in der ersten Nutzergeste, damit auch per API/
  // Streamdeck ausgelöste Wiedergabe (nicht in einer Geste) Ton macht.
  function unlockMediaOnce() {
    if (mediaUnlocked) return;
    const el = getMediaEl();
    if (activeId) { mediaUnlocked = true; return; }
    try {
      el.src = SILENT_WAV;
      el.muted = true;
      const p = el.play();
      if (p && p.then) p.then(function () { try { el.pause(); el.currentTime = 0; } catch (e) {} el.muted = false; mediaUnlocked = true; }).catch(function () { el.muted = false; });
      else mediaUnlocked = true;
    } catch (e) {}
  }

  function getMediaEl() {
    if (!mediaEl) {
      mediaEl = new Audio();
      mediaEl.preload = 'auto';
      mediaEl.addEventListener('ended', onLocalEnded);
      mediaEl.addEventListener('error', onLocalEnded);
    }
    return mediaEl;
  }

  function emitActive() {
    if (dotnet) dotnet.invokeMethodAsync('OnActiveChanged', activeId ? [activeId] : []);
  }

  function onLocalEnded() {
    if (activeTimer) { clearTimeout(activeTimer); activeTimer = null; }
    if (activeId !== null) { activeId = null; emitActive(); }
  }

  board.currentLocalId = function () { return activeId; };

  function pauseMedia() {
    if (activeTimer) { clearTimeout(activeTimer); activeTimer = null; }
    if (mediaEl) { try { mediaEl.pause(); } catch {} }
  }

  board.playLocal = function (id, ref, startSec, durationSec) {
    board.stopSequence();
    spotifyId = null;
    if (player) { try { player.pause(); } catch {} }
    pauseMedia();
    const src = /^(https?:)?\//.test(ref) ? ref : assetBase + ref;
    const el = getMediaEl();
    el.volume = volume;
    activeId = id;
    emitActive();
    const begin = function () {
      try { el.currentTime = startSec > 0 ? startSec : 0; } catch (e) {}
      const p = el.play();
      if (p && p.catch) p.catch(function () {});
      if (durationSec) activeTimer = setTimeout(function () { try { el.pause(); } catch (e) {} onLocalEnded(); }, durationSec * 1000);
    };
    if (el.src !== src) {
      el.src = src;
      if (startSec > 0) { el.addEventListener('loadedmetadata', begin, { once: true }); el.load(); }
      else { begin(); }
    } else {
      begin();
    }
  };

  board.stopLocal = function () {
    board.stopSequence();
    pauseMedia();
    if (activeId !== null) { activeId = null; emitActive(); }
  };

  board.setVolume = function (v) {
    volume = v;
    if (mediaEl) mediaEl.volume = v;
    if (player) player.setVolume(v);
  };

  // ---------- Sequenzer: mehrere Songs pro Kachel (Reihenfolge/Zufall, Endlos-Loop) ----------
  let seqToken = 0;
  board.stopSequence = function () { seqToken++; };

  function playOneSong(song, token) {
    return new Promise(function (resolve) {
      if (token !== seqToken) { resolve(); return; }
      if (song.t === 'spotify') {
        if (!board.isLoggedIn()) { if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStatus', 'not-logged-in'); resolve(); return; }
        board.activateSpotify();
        board.playSpotify(song.r, (song.s || 0) * 1000, !!song.sh).then(function (status) {
          if (token !== seqToken) { resolve(); return; }
          if (status !== 'ok') { if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStatus', status); resolve(); return; }
          if (song.d) { setTimeout(function () { resolve(); }, song.d * 1000); }
          else {
            const poll = setInterval(async function () {
              if (token !== seqToken) { clearInterval(poll); resolve(); return; }
              const st = await board.getState();
              if (st && ((st.duration > 0 && st.position >= st.duration - 1500) || (st.paused && st.position === 0))) { clearInterval(poll); resolve(); }
            }, 1000);
          }
        });
      } else {
        const el = getMediaEl();
        if (player) { try { player.pause(); } catch {} }
        const src = /^(https?:)?\//.test(song.r) ? song.r : assetBase + song.r;
        el.volume = volume;
        let done = false, cut = null;
        const finish = function () { if (done) return; done = true; el.removeEventListener('ended', onEnd); if (cut) clearTimeout(cut); resolve(); };
        const onEnd = function () { finish(); };
        el.addEventListener('ended', onEnd, { once: true });
        const begin = function () {
          try { el.currentTime = song.s > 0 ? song.s : 0; } catch (e) {}
          const p = el.play(); if (p && p.catch) p.catch(finish);
          if (song.d) cut = setTimeout(function () { try { el.pause(); } catch (e) {} finish(); }, song.d * 1000);
        };
        if (el.src !== src) { el.src = src; if (song.s > 0) { el.addEventListener('loadedmetadata', begin, { once: true }); el.load(); } else begin(); }
        else begin();
      }
    });
  }

  board.playSongs = function (id, songs, random) {
    if (!songs || !songs.length) return;
    board.stopSequence();
    pauseMedia();
    if (player) { try { player.pause(); } catch {} }
    spotifyId = null;
    const my = ++seqToken;
    activeId = id;
    emitActive();
    const loop = songs.length > 1;
    const order = songs.map(function (_, i) { return i; });
    let pos = 0;
    function nextIndex() {
      if (random) return Math.floor(Math.random() * songs.length);
      const i = order[pos % order.length]; pos++; return i;
    }
    (function step() {
      if (my !== seqToken) return;
      const song = songs[nextIndex()];
      playOneSong(song, my).then(function () {
        if (my !== seqToken) return;
        if (loop) { step(); }
        else if (activeId === id) { activeId = null; emitActive(); }
      });
    })();
  };

  // Kachel-Klick: Songs direkt im Klick-Event abspielen (iOS-Nutzergeste).
  document.addEventListener('click', function (e) {
    const tile = e.target && e.target.closest ? e.target.closest('[data-play="songs"]') : null;
    if (!tile) return;
    const id = tile.getAttribute('data-id');
    const force = tile.getAttribute('data-force') === '1';
    let songs = [];
    try { songs = JSON.parse(tile.getAttribute('data-songs') || '[]'); } catch (x) {}
    if (!songs.length) return;
    if (!force && (activeId === id || spotifyId === id)) {
      board.stopAll();
      if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStopped');
      return;
    }
    if (songs.length === 1 && songs[0].t === 'spotify') {
      const s = songs[0];
      if (!board.isLoggedIn()) { if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStatus', 'not-logged-in'); return; }
      board.stopLocal(); board.activateSpotify(); spotifyId = id;
      const label = tile.getAttribute('data-label') || '';
      board.playSpotify(s.r, (s.s || 0) * 1000, !!s.sh).then(function (status) {
        if (status === 'ok') { if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStarted', id, label, s.d != null ? s.d : null); }
        else { spotifyId = null; if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStatus', status); }
      });
    } else if (songs.length === 1) {
      const s = songs[0];
      board.playLocal(id, s.r, s.s || 0, (s.d != null ? s.d : null));
      if (dotnet) { try { dotnet.invokeMethodAsync('OnLocalStarted'); } catch (x) {} }
    } else {
      board.playSongs(id, songs, tile.getAttribute('data-random') === '1');
      if (dotnet) { try { dotnet.invokeMethodAsync('OnLocalStarted'); } catch (x) {} }
    }
  }, true);

  // iOS: der Spotify-Player muss in einer Nutzergeste freigeschaltet werden.
  document.addEventListener('pointerdown', function () {
    board.activateSpotify();
    unlockMediaOnce();
    if (dotnet) { try { dotnet.invokeMethodAsync('OnActivated'); } catch (e) {} }
  }, { passive: true });

  // Long-Press auf Mehr-Song-Kacheln → Einzelsong-Auswahl (Overlay in Blazor).
  (function () {
    let lpTimer = null, lpFired = false, lpX = 0, lpY = 0;
    document.addEventListener('pointerdown', function (e) {
      const tile = e.target && e.target.closest ? e.target.closest('.sb-tile[data-multi="1"]') : null;
      if (!tile) return;
      lpFired = false; lpX = e.clientX; lpY = e.clientY;
      lpTimer = setTimeout(function () {
        lpFired = true;
        if (dotnet) dotnet.invokeMethodAsync('OnLongPress', tile.getAttribute('data-id'));
      }, 500);
    });
    document.addEventListener('pointermove', function (e) {
      if (lpTimer && (Math.abs(e.clientX - lpX) > 12 || Math.abs(e.clientY - lpY) > 12)) { clearTimeout(lpTimer); lpTimer = null; }
    });
    document.addEventListener('pointerup', function () {
      if (lpTimer) { clearTimeout(lpTimer); lpTimer = null; }
      if (lpFired) {
        const sup = function (ev) { ev.stopPropagation(); ev.preventDefault(); };
        document.addEventListener('click', sup, { capture: true, once: true });
        setTimeout(function () { document.removeEventListener('click', sup, { capture: true }); }, 400);
        lpFired = false;
      }
    });
    document.addEventListener('pointercancel', function () { if (lpTimer) { clearTimeout(lpTimer); lpTimer = null; } });
  })();

  // ---------- Spotify: PKCE + Web Playback SDK ----------
  const CLIENT_ID_KEY = 'sb_spotify_client_id';
  const TOKEN_KEY = 'sb_spotify_token';
  const VERIFIER_KEY = 'sb_spotify_verifier';
  const SCOPES = 'streaming user-read-email user-read-private user-modify-playback-state user-read-playback-state';

  let player = null;
  let deviceId = null;
  let spotifyId = null;
  let spotifyActivated = false;

  // iOS-Freischaltung des SDK-Audioelements (muss in einer Nutzergeste passieren).
  board.activateSpotify = function () {
    if (spotifyActivated || !player || typeof player.activateElement !== 'function') return;
    try { player.activateElement(); spotifyActivated = true; } catch (e) {}
  };
  board.currentSpotifyId = function () { return spotifyId; };

  function redirectUri() { return location.origin + appBase + 'callback'; }
  function callbackPath() { return (appBase + 'callback').replace(/\/{2,}/g, '/'); }

  board.getClientId = function () { return localStorage.getItem(CLIENT_ID_KEY) || ''; };
  board.setClientId = function (id) { localStorage.setItem(CLIENT_ID_KEY, (id || '').trim()); };
  board.redirectUri = redirectUri;
  board.isPlayerReady = function () { return deviceId !== null; };

  function loadToken() {
    const raw = localStorage.getItem(TOKEN_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw); } catch { return null; }
  }
  function saveToken(t) { localStorage.setItem(TOKEN_KEY, JSON.stringify(t)); }
  board.isLoggedIn = function () { return loadToken() !== null; };
  board.logout = function () { localStorage.removeItem(TOKEN_KEY); deviceId = null; };

  async function getAccessToken() {
    const token = loadToken();
    if (!token) throw new Error('Nicht mit Spotify verbunden');
    if (Date.now() < token.expiresAt) return token.accessToken;
    const body = new URLSearchParams({
      client_id: board.getClientId(),
      grant_type: 'refresh_token',
      refresh_token: token.refreshToken,
    });
    const res = await fetch('https://accounts.spotify.com/api/token', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body,
    });
    if (!res.ok) { board.logout(); throw new Error('Spotify-Token-Refresh fehlgeschlagen (' + res.status + ')'); }
    const json = await res.json();
    saveToken({
      accessToken: json.access_token,
      refreshToken: json.refresh_token || token.refreshToken,
      expiresAt: Date.now() + json.expires_in * 1000 - 60000,
    });
    return json.access_token;
  }

  function randomString(length) {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    const bytes = crypto.getRandomValues(new Uint8Array(length));
    return Array.from(bytes, (b) => chars[b % chars.length]).join('');
  }
  async function codeChallenge(verifier) {
    const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
    return btoa(String.fromCharCode.apply(null, Array.from(new Uint8Array(digest))))
      .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  }

  board.startLogin = async function () {
    const clientId = board.getClientId();
    if (!clientId) throw new Error('Keine Spotify Client-ID hinterlegt');
    const verifier = randomString(64);
    localStorage.setItem(VERIFIER_KEY, verifier);
    const params = new URLSearchParams({
      client_id: clientId,
      response_type: 'code',
      redirect_uri: redirectUri(),
      scope: SCOPES,
      code_challenge_method: 'S256',
      code_challenge: await codeChallenge(verifier),
    });
    location.href = 'https://accounts.spotify.com/authorize?' + params;
  };

  async function handleCallbackIfPresent() {
    if (location.pathname !== callbackPath()) return false;
    const params = new URLSearchParams(location.search);
    const code = params.get('code');
    const error = params.get('error');
    history.replaceState(null, '', appBase);
    if (error) throw new Error('Spotify-Login abgelehnt: ' + error);
    if (!code) return false;
    const verifier = localStorage.getItem(VERIFIER_KEY);
    if (!verifier) throw new Error('PKCE-Verifier fehlt, bitte Login erneut starten');
    const body = new URLSearchParams({
      client_id: board.getClientId(),
      grant_type: 'authorization_code',
      code,
      redirect_uri: redirectUri(),
      code_verifier: verifier,
    });
    const res = await fetch('https://accounts.spotify.com/api/token', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body,
    });
    if (!res.ok) throw new Error('Spotify-Token-Tausch fehlgeschlagen (' + res.status + ')');
    const json = await res.json();
    saveToken({
      accessToken: json.access_token,
      refreshToken: json.refresh_token,
      expiresAt: Date.now() + json.expires_in * 1000 - 60000,
    });
    localStorage.removeItem(VERIFIER_KEY);
    return true;
  }

  function loadSdk() {
    return new Promise((resolve) => {
      if (window.Spotify) return resolve();
      window.onSpotifyWebPlaybackSDKReady = () => resolve();
      const script = document.createElement('script');
      script.src = 'https://sdk.scdn.co/spotify-player.js';
      document.body.appendChild(script);
    });
  }

  async function initPlayer() {
    if (player) return;
    await loadSdk();
    player = new window.Spotify.Player({
      name: 'Soundboard',
      getOAuthToken: (cb) => { getAccessToken().then(cb).catch(toast); },
      volume,
    });
    player.addListener('ready', ({ device_id }) => {
      deviceId = device_id;
      if (dotnet) dotnet.invokeMethodAsync('OnPlayerReady');
    });
    player.addListener('not_ready', () => { deviceId = null; });
    ['initialization_error', 'authentication_error', 'account_error']
      .forEach((ev) => player.addListener(ev, ({ message }) => toast('Spotify: ' + message)));
    player.addListener('playback_error', ({ message }) => {
      if (/no list was loaded|no list was previously loaded/i.test(message)) return;
      toast('Spotify: ' + message);
    });
    await player.connect();
  }

  async function spotifyApi(path, init) {
    const token = await getAccessToken();
    const res = await fetch('https://api.spotify.com/v1' + path, {
      ...(init || {}),
      headers: { ...((init && init.headers) || {}), Authorization: 'Bearer ' + token },
    });
    if (!res.ok && res.status !== 204) {
      let detail = '';
      try {
        const j = await res.json();
        if (j && j.error) detail = [j.error.reason, j.error.message].filter(Boolean).join(' - ');
      } catch {}
      throw new Error('Spotify-API-Fehler ' + res.status + (detail ? ': ' + detail : ''));
    }
    return res;
  }

  async function transferToDevice() {
    await spotifyApi('/me/player', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ device_ids: [deviceId], play: false }),
    });
  }

  async function playBody(body) {
    await spotifyApi('/me/player/play?device_id=' + deviceId, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
  }

  board.playSpotify = async function (uri, positionMs, shuffle) {
    if (!board.isLoggedIn()) return 'not-logged-in';
    if (!deviceId) return 'not-ready';
    pauseMedia();
    board.activateSpotify();
    var isContext = /^spotify:(playlist|album|artist):/.test(uri);
    var body = isContext
      ? (shuffle ? { context_uri: uri, position_ms: positionMs } : { context_uri: uri, offset: { position: 0 }, position_ms: positionMs })
      : { uris: [uri], position_ms: positionMs };
    try {
      if (player) { try { await player.setVolume(volume); } catch (x) {} }
      // SDK-Gerät zum aktiven Gerät machen (behebt geräteabhängige 403).
      try { await transferToDevice(); await new Promise(function (r) { setTimeout(r, 400); }); } catch (x) {}
      if (isContext) {
        try { await spotifyApi('/me/player/shuffle?state=' + (shuffle ? 'true' : 'false') + '&device_id=' + deviceId, { method: 'PUT' }); } catch (x) {}
      }
      await playBody(body);
      return 'ok';
    } catch (e) {
      // Ein Retry nach erneutem Transfer (Gerät war evtl. noch nicht aktiv).
      try {
        await transferToDevice();
        await new Promise(function (r) { setTimeout(r, 700); });
        await playBody(body);
        return 'ok';
      } catch (e2) {
        return e2 instanceof Error ? e2.message : String(e2);
      }
    }
  };

  board.diagnose = async function () {
    const out = [];
    try {
      const me = await (await spotifyApi('/me', {})).json();
      out.push('Account: ' + (me.id || '?') + '  Produkt: ' + (me.product || '?') + '  Land: ' + (me.country || '?'));
    } catch (e) { out.push('/me FEHLER: ' + (e && e.message ? e.message : e)); }
    out.push('SDK deviceId: ' + (deviceId || 'null'));
    try {
      const d = await (await spotifyApi('/me/player/devices', {})).json();
      const list = (d.devices || []).map(function (x) { return x.name + ' [active=' + x.is_active + ' restricted=' + x.is_restricted + ']'; });
      out.push('Geräte: ' + (list.length ? list.join(', ') : 'keine'));
    } catch (e) { out.push('/me/player/devices FEHLER: ' + (e && e.message ? e.message : e)); }
    try { await transferToDevice(); out.push('Transfer aufs SDK-Gerät: OK'); }
    catch (e) { out.push('Transfer FEHLER: ' + (e && e.message ? e.message : e)); }
    return out.join('\n');
  };

  board.stopSpotify = async function (fade) {
    spotifyId = null;
    try {
      if (fade && player) {
        const steps = 10;
        for (let i = steps - 1; i >= 0; i--) {
          await player.setVolume(volume * (i / steps));
          await new Promise((r) => setTimeout(r, 150));
        }
        await player.pause();
        await player.setVolume(volume);
      } else if (player) {
        await player.pause();
        await player.setVolume(volume);
      }
    } catch {}
  };

  async function fetchDisplayName() {
    const res = await spotifyApi('/me', {});
    const json = await res.json();
    return json.display_name || json.id;
  }

  function toast(e) {
    const msg = e instanceof Error ? e.message : String(e);
    if (dotnet) dotnet.invokeMethodAsync('OnToast', msg);
  }

  board.stopAll = function () { board.stopLocal(); void board.stopSpotify(false); };

  board.pause = function () {
    if (mediaEl && activeId) { try { mediaEl.pause(); } catch {} if (activeTimer) { clearTimeout(activeTimer); activeTimer = null; } }
    if (player) { try { player.pause(); } catch {} }
  };

  board.resume = function () {
    if (mediaEl && activeId) { const p = mediaEl.play(); if (p && p.catch) p.catch(function () {}); }
    if (player) { try { player.resume(); } catch {} }
  };

  board.fadeOut = async function () {
    if (mediaEl && activeId) {
      const steps = 12;
      for (let i = steps - 1; i >= 0; i--) {
        try { mediaEl.volume = volume * (i / steps); } catch {}
        await new Promise(function (r) { setTimeout(r, 90); });
      }
      board.stopLocal();
      try { mediaEl.volume = volume; } catch {}
    }
    if (player) { await board.stopSpotify(true); }
  };

  board.getState = async function () {
    try { return player ? await player.getCurrentState() : null; } catch (e) { return null; }
  };

  board.seek = async function (frac) {
    try { if (!player) return; const st = await player.getCurrentState(); if (st) await player.seek(Math.floor(frac * (st.duration || 0))); } catch (e) { }
  };

  // Stellt sicher, dass der Spotify-Player bereit ist (für den Editor-Test).
  board.ensureSpotify = async function () {
    if (!board.isLoggedIn()) return false;
    if (deviceId) return true;
    try { await initPlayer(); } catch (e) { return false; }
    for (var i = 0; i < 50 && !deviceId; i++) { await new Promise(function (r) { setTimeout(r, 100); }); }
    return deviceId !== null;
  };

  let escBound = false;
  board.init = async function (ref, assetBaseUrl, appBaseUrl) {
    dotnet = ref;
    assetBase = assetBaseUrl || '';
    appBase = appBaseUrl || '/show/';
    if (!escBound) {
      escBound = true;
      document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && dotnet) dotnet.invokeMethodAsync('OnEscape');
      });
    }
    let logged = board.isLoggedIn();
    try {
      const justLoggedIn = await handleCallbackIfPresent();
      logged = justLoggedIn || board.isLoggedIn();
      if (logged) {
        void initPlayer();
        fetchDisplayName().then((n) => dotnet && dotnet.invokeMethodAsync('OnDisplayName', n)).catch(() => {});
      }
    } catch (e) {
      toast(e);
    }
    return { loggedIn: logged, hasClientId: !!board.getClientId(), redirectUri: redirectUri() };
  };

  board.copyText = async function (text) {
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) { await navigator.clipboard.writeText(text); return true; }
    } catch (e) {}
    try {
      const ta = document.createElement('textarea');
      ta.value = text; ta.setAttribute('readonly', '');
      ta.style.position = 'fixed'; ta.style.top = '0'; ta.style.opacity = '0';
      document.body.appendChild(ta); ta.focus(); ta.select();
      ta.setSelectionRange(0, ta.value.length);
      const ok = document.execCommand('copy');
      ta.remove();
      return ok;
    } catch (e) { return false; }
  };

  window.showBoard = board;
})();
