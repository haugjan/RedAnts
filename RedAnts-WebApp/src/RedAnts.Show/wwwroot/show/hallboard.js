(function () {
  const board = {};
  const minCell = 44;
  const maxCell = 170;
  const minFont = 8;
  let dotnet = null;
  let frame = 0;
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
    layout();
    window.addEventListener('resize', layout);
  };

  board.play = function (id, songsJson, random, label) {
    const dj = player();
    if (!dj) return;
    if (dj.currentLocalId && dj.currentLocalId() === id) { announce(label || ''); return; }
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

  board.control = function (what) {
    if (what === 'stop') board.stop();
    else if (what === 'fade') board.fade();
    else if (what === 'next') board.next();
    else if (what === 'previous') board.previous();
    else if (what === 'pause') board.pause();
  };

  function rowsOf(grid) {
    const columns = Number(getComputedStyle(grid).getPropertyValue('--cols')) || 1;
    const keys = grid.querySelectorAll('.key').length;
    return Math.max(1, Math.ceil(keys / columns));
  }

  function cellSize(grids) {
    const root = document.querySelector('.hb');
    const main = root && root.querySelector('main');
    if (!main) return maxCell;
    const bar = root.querySelector('.bar');
    const now = root.querySelector('.now');
    const hint = main.querySelector('.hint');
    const note = main.querySelector('.note');
    const style = getComputedStyle(main);
    let taken = parseFloat(style.paddingTop) + parseFloat(style.paddingBottom) + 4;
    [bar, now, hint, note].forEach(function (part) { if (part) taken += part.offsetHeight + 6; });
    let rows = 0;
    grids.forEach(function (grid) {
      const own = rowsOf(grid);
      rows += own;
      taken += (own - 1) * 8 + 14;
      const head = grid.parentElement && grid.parentElement.querySelector('h2');
      if (head) taken += head.offsetHeight + 6;
    });
    if (rows === 0) return maxCell;
    const room = (window.innerHeight - taken) / rows;
    return Math.max(minCell, Math.min(maxCell, Math.floor(room)));
  }

  function overflows(key) {
    const label = key.querySelector('.label');
    if (!label) return false;
    return label.scrollHeight > label.clientHeight + 1 || label.scrollWidth > label.clientWidth + 1;
  }

  function fitGrid(grid) {
    const keys = Array.from(grid.querySelectorAll('.key'));
    const labelled = keys.filter(function (key) {
      const label = key.querySelector('.label');
      return label && label.textContent.trim().length > 0;
    });
    if (!labelled.length) return;
    const sample = keys[0];
    let size = Math.max(minFont, Math.min(Math.round(sample.clientWidth / 5), Math.round(sample.clientHeight / 4)));
    const apply = function (value) {
      labelled.forEach(function (key) { key.querySelector('.label').style.fontSize = value + 'px'; });
    };
    apply(size);
    let guard = 0;
    while (size > minFont && guard++ < 48 && labelled.some(overflows)) {
      size -= 1;
      apply(size);
    }
  }

  function layout() {
    if (frame) cancelAnimationFrame(frame);
    frame = requestAnimationFrame(function () {
      frame = 0;
      const grids = Array.from(document.querySelectorAll('.hb .grid'));
      if (!grids.length) return;
      const cell = cellSize(grids);
      grids.forEach(function (grid) { grid.style.setProperty('--cell', cell + 'px'); });
      grids.forEach(fitGrid);
    });
  }

  board.fit = layout;

  const observer = new MutationObserver(layout);
  function watch() {
    const root = document.querySelector('.hb');
    if (root) observer.observe(root, { childList: true, subtree: true, characterData: true });
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', watch);
  else watch();

  window.hallBoard = board;
})();
