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
  let volume = loadVolume();
  let mediaEl = null;
  let audioCtx = null;
  let gainNode = null;
  let activeId = null;
  let activeTimer = null;
  let mediaUnlocked = false;

  // Einmalige Audio-Freischaltung in der ersten Nutzergeste, damit auch per API/
  // Streamdeck ausgelöste Wiedergabe (nicht in einer Geste) Ton macht.
  function unlockMediaOnce() {
    if (mediaUnlocked) return;
    mediaUnlocked = true;
    const el = getMediaEl();
    routeThroughGain(el);
    if (activeId) return;
    try {
      el.src = SILENT_WAV;
      const silentSrc = el.src;
      el.muted = true;
      const p = el.play();
      if (p && p.then) p.then(function () {
        if (el.src === silentSrc && !activeId) { try { el.pause(); el.currentTime = 0; } catch (e) {} }
        el.muted = false;
      }).catch(function () { el.muted = false; });
    } catch (e) { el.muted = false; }
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

  function loadVolume() {
    try {
      const v = parseFloat(localStorage.getItem('sb_volume'));
      return isFinite(v) && v >= 0 && v <= 1 ? v : 0.9;
    } catch (e) { return 0.9; }
  }

  function volumeIsFixed(el) {
    try {
      const before = el.volume;
      el.volume = before > 0.5 ? 0.25 : 0.75;
      const fixed = el.volume === before;
      el.volume = before;
      return fixed;
    } catch (e) { return true; }
  }

  function routeThroughGain(el) {
    if (gainNode || !volumeIsFixed(el)) return;
    const Ctx = window.AudioContext || window.webkitAudioContext;
    if (!Ctx) return;
    try {
      audioCtx = new Ctx();
      const gain = audioCtx.createGain();
      gain.gain.value = volume;
      audioCtx.createMediaElementSource(el).connect(gain);
      gain.connect(audioCtx.destination);
      gainNode = gain;
    } catch (e) { gainNode = null; }
  }

  function setLocalLevel(level) {
    if (gainNode) { gainNode.gain.value = level; return; }
    if (mediaEl) { try { mediaEl.volume = level; } catch (e) {} }
  }

  function resumeAudio() {
    if (audioCtx && audioCtx.state !== 'running') { try { audioCtx.resume(); } catch (e) {} }
  }

  function emitActive() {
    if (dotnet) dotnet.invokeMethodAsync('OnActiveChanged', activeId ? [activeId] : []);
  }

  function onLocalEnded() {
    if (activeTimer) { clearTimeout(activeTimer); activeTimer = null; }
    if (activeId !== null) { activeId = null; emitActive(); }
  }

  board.currentLocalId = function () { return activeId; };

  let pendingReady = null;
  let playToken = 0;

  function cancelPendingReady() {
    if (!pendingReady) return;
    pendingReady.el.removeEventListener('loadedmetadata', pendingReady.handler);
    pendingReady = null;
  }

  function resolveSrc(ref) {
    return /^(https?:)?\//.test(ref) ? ref : assetBase + ref;
  }

  function isLoaded(el, src) {
    if (!el.currentSrc) return false;
    try { return el.currentSrc === new URL(src, location.href).href; }
    catch (e) { return el.src === src; }
  }

  function playAt(el, src, startSec, afterStart, onFail) {
    cancelPendingReady();
    const token = ++playToken;
    const begin = function () {
      if (token !== playToken) return;
      el.muted = false;
      resumeAudio();
      const target = startSec > 0 ? startSec : 0;
      try { el.currentTime = target; }
      catch (e) { if (onFail) { onFail(); return; } }
      const p = el.play();
      if (p && p.catch) p.catch(function () { if (onFail) onFail(); });
      if (afterStart) afterStart();
    };
    if (isLoaded(el, src) && el.readyState >= 1) { begin(); return; }
    const handler = function () { pendingReady = null; begin(); };
    pendingReady = { el: el, handler: handler };
    el.addEventListener('loadedmetadata', handler, { once: true });
    if (isLoaded(el, src)) el.load(); else el.src = src;
  }

  function pauseMedia() {
    cancelPendingReady();
    playToken++;
    if (activeTimer) { clearTimeout(activeTimer); activeTimer = null; }
    if (mediaEl) { try { mediaEl.pause(); } catch {} }
  }

  board.playLocal = function (id, ref, startSec, durationSec, title, artist) {
    board.stopSequence();
    armSongInfo(true);
    spotifyId = null;
    spotifyContext = false;
    cueStart = startSec || 0;
    if (player) { try { player.pause(); } catch {} }
    pauseMedia();
    const el = getMediaEl();
    setLocalLevel(volume);
    activeId = id;
    emitActive();
    reportSong(title || fileLabel(ref), artist || '');
    playAt(el, resolveSrc(ref), startSec, function () {
      if (durationSec) activeTimer = setTimeout(function () { try { el.pause(); } catch (e) {} onLocalEnded(); }, durationSec * 1000);
    }, onLocalEnded);
  };

  board.stopLocal = function () {
    board.stopSequence();
    pauseMedia();
    armSongInfo(false);
    if (activeId !== null) { activeId = null; emitActive(); }
  };

  board.setVolume = function (v) {
    const next = Math.min(1, Math.max(0, Number(v)));
    if (!isFinite(next)) return;
    volume = next;
    try { localStorage.setItem('sb_volume', String(volume)); } catch (e) {}
    setLocalLevel(volume);
    if (player) { try { player.setVolume(volume); } catch (e) {} }
    queueSpotifyVolume();
  };

  let spotifyVolumeTimer = null;

  function queueSpotifyVolume() {
    if (!deviceId || !board.isLoggedIn()) return;
    if (spotifyVolumeTimer) clearTimeout(spotifyVolumeTimer);
    spotifyVolumeTimer = setTimeout(function () {
      spotifyVolumeTimer = null;
      spotifyApi('/me/player/volume?volume_percent=' + Math.round(volume * 100) + '&device_id=' + deviceId, { method: 'PUT' }).catch(function () {});
    }, 500);
  }

  // ---------- laufender Song für die Fussleiste ----------
  let songInfoOn = false;
  let lastSongInfo = '';

  function isContextRef(ref) { return /^spotify:(playlist|album|artist):/.test(String(ref || '')); }

  function fileLabel(ref) {
    let r = String(ref || '');
    const q = r.indexOf('?');
    if (q >= 0) r = r.slice(0, q);
    const slash = r.lastIndexOf('/');
    if (slash >= 0) r = r.slice(slash + 1);
    try { r = decodeURIComponent(r); } catch (e) {}
    return r.replace(/\.[a-z0-9]+$/i, '');
  }

  function reportSong(title, sub) {
    if (!songInfoOn || !dotnet) return;
    const key = (title || '') + '|' + (sub || '');
    if (key === lastSongInfo) return;
    lastSongInfo = key;
    try { dotnet.invokeMethodAsync('OnSongInfo', title || '', sub || ''); } catch (e) {}
  }

  function armSongInfo(on) {
    songInfoOn = !!on;
    lastSongInfo = '';
    if (!on && dotnet) { try { dotnet.invokeMethodAsync('OnSongInfo', '', ''); } catch (e) {} }
  }

  // ---------- Sequenzer: mehrere Songs pro Kachel (Reihenfolge/Zufall, Endlos-Loop) ----------
  let seqToken = 0;
  let seq = null;
  let cueStart = 0;
  let spotifyContext = false;
  const RESTART_WINDOW_SEC = 3;
  const HISTORY_LIMIT = 200;
  board.stopSequence = function () { seqToken++; seq = null; };

  function stopSequenceOnError() {
    seqToken++;
    seq = null;
    armSongInfo(false);
    if (activeId !== null) { activeId = null; emitActive(); }
    if (dotnet) { try { dotnet.invokeMethodAsync('OnSpotifyStopped'); } catch (e) {} }
  }

  function playOneSong(song, token) {
    return new Promise(function (resolve) {
      if (token !== seqToken) { resolve(); return; }
      if (song.t === 'spotify') {
        if (!board.isLoggedIn()) { if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStatus', 'not-logged-in'); stopSequenceOnError(); resolve(); return; }
        board.activateSpotify();
        board.playSpotify(song.r, (song.s || 0) * 1000, !!song.sh).then(function (status) {
          if (token !== seqToken) { resolve(); return; }
          if (status !== 'ok') {
            if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStatus', status);
            resolve(false);
            return;
          }
          if (song.d) { setTimeout(function () { resolve(true); }, song.d * 1000); }
          else {
            const poll = setInterval(async function () {
              if (token !== seqToken) { clearInterval(poll); resolve(true); return; }
              const st = await board.getState();
              if (st && ((st.duration > 0 && st.position >= st.duration - 1500) || (st.paused && st.position === 0))) { clearInterval(poll); resolve(true); }
            }, 1000);
          }
        });
      } else {
        const el = getMediaEl();
        if (player) { try { player.pause(); } catch {} }
        setLocalLevel(volume);
        let done = false, cut = null;
        const finish = function () { if (done) return; done = true; el.removeEventListener('ended', onEnd); if (cut) clearTimeout(cut); resolve(true); };
        const onEnd = function () { finish(); };
        el.addEventListener('ended', onEnd, { once: true });
        reportSong(song.ti || fileLabel(song.r), song.ar || '');
        playAt(el, resolveSrc(song.r), song.s, function () {
          if (song.d) cut = setTimeout(function () { try { el.pause(); } catch (e) {} finish(); }, song.d * 1000);
        }, finish);
      }
    });
  }

  function newSequence(id, songs, random) {
    return { id: id, songs: songs, random: !!random, bag: [], history: [], at: -1, current: null, fails: 0 };
  }

  function shuffledIndices(count, avoidFirst) {
    const bag = [];
    for (let i = 0; i < count; i++) bag.push(i);
    for (let i = count - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      const t = bag[i]; bag[i] = bag[j]; bag[j] = t;
    }
    if (count > 1 && bag[count - 1] === avoidFirst) { const t = bag[0]; bag[0] = bag[count - 1]; bag[count - 1] = t; }
    return bag;
  }

  function drawIndex(s) {
    const last = s.at >= 0 ? s.history[s.at] : -1;
    if (!s.random) return (last + 1) % s.songs.length;
    if (!s.bag.length) s.bag = shuffledIndices(s.songs.length, last);
    return s.bag.pop();
  }

  function advance(s) {
    if (s.at < s.history.length - 1) { s.at++; return s.history[s.at]; }
    s.history.push(drawIndex(s));
    if (s.history.length > HISTORY_LIMIT) s.history.shift();
    s.at = s.history.length - 1;
    return s.history[s.at];
  }

  function retreat(s) {
    if (s.at > 0) s.at--;
    return s.history[s.at];
  }

  function playIndex(index) {
    const s = seq;
    if (!s) return;
    const my = ++seqToken;
    s.current = index;
    playOneSong(s.songs[index], my).then(function (played) {
      if (my !== seqToken || seq !== s) return;
      if (played === false) {
        s.fails++;
        if (s.fails >= 3) { stopSequenceOnError(); return; }
        setTimeout(function () { if (my === seqToken && seq === s) playIndex(advance(s)); }, 1500);
        return;
      }
      s.fails = 0;
      if (s.songs.length > 1) playIndex(advance(s));
      else if (activeId === s.id) { activeId = null; emitActive(); }
    });
  }

  board.playSongs = function (id, songs, random) {
    if (!songs || !songs.length) return;
    board.stopSequence();
    pauseMedia();
    if (player) { try { player.pause(); } catch {} }
    spotifyId = null;
    spotifyContext = false;
    activeId = id;
    emitActive();
    armSongInfo(true);
    seq = newSequence(id, songs, random);
    playIndex(advance(seq));
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
      spotifyContext = isContextRef(s.r);
      cueStart = s.s || 0;
      armSongInfo(true);
      const label = tile.getAttribute('data-label') || '';
      board.playSpotify(s.r, (s.s || 0) * 1000, !!s.sh).then(function (status) {
        if (status === 'ok') { if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStarted', id, label, s.d != null ? s.d : null); }
        else { spotifyId = null; if (dotnet) dotnet.invokeMethodAsync('OnSpotifyStatus', status); }
      });
    } else if (songs.length === 1) {
      const s = songs[0];
      board.playLocal(id, s.r, s.s || 0, (s.d != null ? s.d : null), s.ti, s.ar);
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
    resumeAudio();
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
  let transferredFor = null;
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
  board.isLoggedIn = function () { return serverTokenUrl !== null || loadToken() !== null; };
  board.logout = function () { localStorage.removeItem(TOKEN_KEY); deviceId = null; };

  // Der Admin-Bereich spielt über das zentral verbundene Spotify-Konto: der Token
  // kommt vom Server statt aus dem PKCE-Login des Boards.
  let serverTokenUrl = null;
  let serverToken = null;
  board.useServerTokens = function (url) { serverTokenUrl = url || null; };

  async function getServerToken() {
    if (serverToken && Date.now() < serverToken.expiresAt) return serverToken.accessToken;
    const res = await fetch(serverTokenUrl, { headers: { Accept: 'application/json' } });
    if (!res.ok) throw new Error('Kein Spotify-Konto verbunden (Dialog „Spotify" im Admin)');
    const json = await res.json();
    serverToken = {
      accessToken: json.access_token,
      expiresAt: Date.now() + (json.expires_in || 300) * 1000 - 30000,
    };
    return serverToken.accessToken;
  }

  async function getAccessToken() {
    if (serverTokenUrl) return getServerToken();
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
    player.addListener('not_ready', () => { deviceId = null; transferredFor = null; });
    player.addListener('player_state_changed', (state) => {
      const track = state && state.track_window && state.track_window.current_track;
      if (!track) return;
      reportSong(track.name || '', (track.artists || []).map((a) => a.name).filter(Boolean).join(', '));
    });
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
      if (res.status === 429) {
        const wait = parseInt(res.headers.get('Retry-After') || '0', 10);
        const err429 = new Error('Spotify drosselt gerade (429)' + (wait ? ', in ' + wait + ' Sekunden nochmals versuchen.' : ', kurz warten und nochmals versuchen.'));
        err429.status = 429;
        throw err429;
      }
      const err = new Error('Spotify-API-Fehler ' + res.status + (detail ? ': ' + detail : ''));
      err.status = res.status;
      throw err;
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
      // SDK-Gerät einmal zum aktiven Gerät machen (behebt geräteabhängige 403). Bei jedem
      // Song zu transferieren würde eine lange Songliste unnötig durch die API-Limits jagen.
      if (transferredFor !== deviceId) {
        try {
          await transferToDevice();
          transferredFor = deviceId;
          await new Promise(function (r) { setTimeout(r, 400); });
        } catch (x) {}
      }
      if (isContext) {
        try { await spotifyApi('/me/player/shuffle?state=' + (shuffle ? 'true' : 'false') + '&device_id=' + deviceId, { method: 'PUT' }); } catch (x) {}
      }
      await playBody(body);
      return 'ok';
    } catch (e) {
      // Bei einer Drosselung würde ein zweiter Versuch das Limit nur weiter belasten.
      if (e && e.status === 429) return e.message;
      // Ein Retry nach erneutem Transfer (Gerät war evtl. noch nicht aktiv).
      try {
        await transferToDevice();
        transferredFor = deviceId;
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
    spotifyContext = false;
    armSongInfo(false);
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
        setLocalLevel(volume * (i / steps));
        await new Promise(function (r) { setTimeout(r, 90); });
      }
      board.stopLocal();
      setLocalLevel(volume);
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
    return { loggedIn: logged, hasClientId: !!board.getClientId(), redirectUri: redirectUri(), volume: volume };
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

  function currentSong() { return seq && seq.current !== null ? seq.songs[seq.current] : null; }

  function playingSpotify() {
    const s = currentSong();
    return spotifyId !== null || (!!s && s.t === 'spotify');
  }

  function playingContext() {
    const s = currentSong();
    return s ? s.t === 'spotify' && isContextRef(s.r) : spotifyContext;
  }

  function currentStartSec() {
    if (playingContext()) return 0;
    const s = currentSong();
    return s ? (s.s || 0) : cueStart;
  }

  async function readPosition() {
    if (playingSpotify()) {
      const st = await board.getState();
      return st && st.duration > 0 ? { pos: st.position / 1000, dur: st.duration / 1000 } : null;
    }
    if (mediaEl && activeId && isFinite(mediaEl.duration) && mediaEl.duration > 0) return { pos: mediaEl.currentTime, dur: mediaEl.duration };
    return null;
  }

  async function seekTo(sec) {
    if (playingSpotify()) { if (player) { try { await player.seek(Math.max(0, Math.floor(sec * 1000))); } catch (e) {} } return; }
    if (mediaEl) { try { mediaEl.currentTime = Math.max(0, sec); } catch (e) {} }
  }

  async function restartCurrent() {
    await seekTo(currentStartSec());
    if (!playingSpotify() && mediaEl && mediaEl.paused && activeId) { const p = mediaEl.play(); if (p && p.catch) p.catch(function () {}); }
  }

  board.previous = async function () {
    const p = await readPosition();
    if (p && p.pos - currentStartSec() > RESTART_WINDOW_SEC) { await restartCurrent(); return; }
    if (playingContext() && player) { try { await player.previousTrack(); } catch (e) {} return; }
    if (seq && seq.songs.length > 1) { playIndex(retreat(seq)); return; }
    await restartCurrent();
  };

  board.next = async function () {
    if (playingContext() && player) { try { await player.nextTrack(); } catch (e) {} return; }
    if (seq && seq.songs.length > 1) playIndex(advance(seq));
  };

  let seekDragging = false;
  let seekDuration = 0;

  function clock(sec) {
    const total = Math.max(0, Math.floor(sec || 0));
    return Math.floor(total / 60) + ':' + String(total % 60).padStart(2, '0');
  }

  async function refreshSeek() {
    const wrap = document.getElementById('sb-seek-wrap');
    if (!wrap) return;
    const p = await readPosition();
    wrap.hidden = !p;
    if (!p || seekDragging) return;
    seekDuration = p.dur;
    document.getElementById('sb-seek').value = String(Math.round(p.pos / p.dur * 1000));
    document.getElementById('sb-seek-pos').textContent = clock(p.pos);
    document.getElementById('sb-seek-dur').textContent = clock(p.dur);
  }

  setInterval(function () { void refreshSeek(); }, 500);

  document.addEventListener('input', function (e) {
    const t = e.target;
    if (!t) return;
    if (t.id === 'sb-volume') board.setVolume(parseFloat(t.value));
    if (t.id === 'sb-seek') {
      seekDragging = true;
      const pos = document.getElementById('sb-seek-pos');
      if (pos) pos.textContent = clock(Number(t.value) / 1000 * seekDuration);
    }
  });

  document.addEventListener('change', function (e) {
    const t = e.target;
    if (!t || t.id !== 'sb-seek') return;
    seekTo(Number(t.value) / 1000 * seekDuration).finally(function () { seekDragging = false; });
  });

  window.showBoard = board;
})();
