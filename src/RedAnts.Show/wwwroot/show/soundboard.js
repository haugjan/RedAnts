// Browser-Interop fürs Soundboard. Alle UI-/Ablauflogik liegt in Blazor/C#;
// hier stehen nur die unvermeidbaren Browser-APIs: HTML-Audio (lokale Effekte)
// und das Spotify Web Playback SDK samt PKCE-Login.
(function () {
  const board = {};
  let dotnet = null;
  let assetBase = '';
  let appBase = '/show/';

  // ---------- lokale Audio-Engine (Effekte dürfen sich überlagern) ----------
  const active = new Set();
  const counts = new Map();
  let volume = 0.9;

  function emitActive() {
    if (dotnet) dotnet.invokeMethodAsync('OnActiveChanged', Array.from(counts.keys()));
  }

  board.playLocal = function (id, ref, startSec, durationSec) {
    // Nur ein Sound gleichzeitig: Laufendes (lokal + Spotify) sofort stoppen.
    board.stopLocal();
    if (player) { try { player.pause(); } catch {} }
    const src = /^(https?:)?\//.test(ref) ? ref : assetBase + ref;
    const audio = new Audio(src);
    audio.volume = volume;
    if (startSec > 0) audio.currentTime = startSec;
    const entry = { id, audio, timer: null };
    const finish = () => {
      if (!active.has(entry)) return;
      active.delete(entry);
      if (entry.timer) clearTimeout(entry.timer);
      const c = (counts.get(id) || 1) - 1;
      if (c <= 0) counts.delete(id); else counts.set(id, c);
      emitActive();
    };
    audio.addEventListener('ended', finish);
    audio.addEventListener('error', finish);
    active.add(entry);
    counts.set(id, (counts.get(id) || 0) + 1);
    emitActive();
    audio.play().catch(finish);
    if (durationSec) entry.timer = setTimeout(() => { audio.pause(); finish(); }, durationSec * 1000);
  };

  board.stopLocal = function () {
    for (const e of active) { if (e.timer) clearTimeout(e.timer); e.audio.pause(); }
    active.clear();
    counts.clear();
    emitActive();
  };

  board.setVolume = function (v) {
    volume = v;
    for (const e of active) e.audio.volume = v;
    if (player) player.setVolume(v);
  };

  // ---------- Spotify: PKCE + Web Playback SDK ----------
  const CLIENT_ID_KEY = 'sb_spotify_client_id';
  const TOKEN_KEY = 'sb_spotify_token';
  const VERIFIER_KEY = 'sb_spotify_verifier';
  const SCOPES = 'streaming user-read-email user-read-private user-modify-playback-state user-read-playback-state';

  let player = null;
  let deviceId = null;

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
    ['initialization_error', 'authentication_error', 'account_error', 'playback_error']
      .forEach((ev) => player.addListener(ev, ({ message }) => toast('Spotify: ' + message)));
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
      try { const j = await res.json(); detail = (j && j.error && j.error.message) || ''; } catch {}
      throw new Error('Spotify-API-Fehler ' + res.status + (detail ? ': ' + detail : ''));
    }
    return res;
  }

  board.playSpotify = async function (uri, positionMs) {
    if (!board.isLoggedIn()) return 'not-logged-in';
    if (!deviceId) return 'not-ready';
    board.stopLocal();
    try {
      if (player) await player.setVolume(volume);
      var isContext = /^spotify:(playlist|album|artist):/.test(uri);
      var body = isContext
        ? { context_uri: uri, offset: { position: 0 }, position_ms: positionMs }
        : { uris: [uri], position_ms: positionMs };
      await spotifyApi('/me/player/play?device_id=' + deviceId, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      return 'ok';
    } catch (e) {
      return e instanceof Error ? e.message : String(e);
    }
  };

  board.stopSpotify = async function (fade) {
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

  window.showBoard = board;
})();
