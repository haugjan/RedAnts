(function () {
  const board = {};
  let dotnet = null;
  let fitFrame = 0;
  let paused = false;

  function player() {
    return window.djBoard || null;
  }

  function announce(label) {
    if (!dotnet) return;
    try { dotnet.invokeMethodAsync('Playing', label || ''); } catch (e) { }
  }

  board.start = async function (ref) {
    dotnet = ref;
    const dj = player();
    if (dj && dj.init) {
      try { await dj.init(null, '/dj/sound/', ''); } catch (e) { }
    }
    document.addEventListener('click', function (e) {
      const tile = e.target && e.target.closest ? e.target.closest('[data-play="songs"]') : null;
      if (!tile) return;
      announce(tile.getAttribute('data-label') || '');
    }, true);
    fit();
    window.addEventListener('resize', fit);
  };

  board.play = function (id, songsJson, random, label) {
    const dj = player();
    if (!dj) return;
    let songs = [];
    try { songs = JSON.parse(songsJson || '[]'); } catch (e) { return; }
    if (!songs.length) return;
    dj.playSongs(id, songs, !!random);
    paused = false;
    announce(label || '');
  };

  board.stop = function () {
    const dj = player();
    if (dj) dj.stopAll();
    paused = false;
    announce('');
  };

  board.fade = function () {
    const dj = player();
    if (dj && dj.fadeOut) dj.fadeOut();
  };

  board.next = function () {
    const dj = player();
    if (dj && dj.next) dj.next();
  };

  board.previous = function () {
    const dj = player();
    if (dj && dj.previous) dj.previous();
  };

  board.pause = function () {
    const dj = player();
    if (!dj) return;
    if (paused) { dj.resume(); paused = false; } else { dj.pause(); paused = true; }
  };

  function fitOne(key) {
    const label = key.querySelector('.label');
    if (!label) return;
    const room = key.clientHeight - 10;
    const wide = key.clientWidth - 10;
    let size = Math.max(10, Math.min(key.clientWidth / 5.5, key.clientHeight / 4.5));
    label.style.fontSize = size + 'px';
    let guard = 0;
    while ((label.scrollHeight > room || label.scrollWidth > wide) && size > 8 && guard++ < 40) {
      size -= 1;
      label.style.fontSize = size + 'px';
    }
  }

  function fit() {
    if (fitFrame) cancelAnimationFrame(fitFrame);
    fitFrame = requestAnimationFrame(function () {
      fitFrame = 0;
      document.querySelectorAll('.hb .key').forEach(fitOne);
    });
  }

  board.fit = fit;

  const observer = new MutationObserver(fit);
  document.addEventListener('DOMContentLoaded', function () {
    const main = document.querySelector('.hb');
    if (main) observer.observe(main, { childList: true, subtree: true, characterData: true });
  });

  window.hallBoard = board;
})();
