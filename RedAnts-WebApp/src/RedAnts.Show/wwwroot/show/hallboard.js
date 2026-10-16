(function () {
  const board = {};
  const minCell = 70;
  const maxCell = 170;
  const minFont = 8;
  let dotnet = null;
  let frame = 0;
  let laid = '';
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
    observer.observe(document.body, { childList: true, subtree: true });
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

  const gap = 8;

  function colsOf(grid) {
    return Number(getComputedStyle(grid).getPropertyValue('--cols')) || 1;
  }

  function rowsOf(grid) {
    const keys = grid.querySelectorAll('.key').length;
    return Math.max(1, Math.ceil(keys / colsOf(grid)));
  }

  function byHeight(grids) {
    const root = document.querySelector('.hb');
    const main = root && root.querySelector('main');
    if (!main) return maxCell;
    const bar = root.querySelector('.bar');
    const now = root.querySelector('.now');
    const hint = main.querySelector('.hint');
    const note = main.querySelector('.note');
    const panel = main.querySelector('.panel');
    const style = getComputedStyle(main);
    let taken = parseFloat(style.paddingTop) + parseFloat(style.paddingBottom) + 4;
    [bar, now, hint, note, panel].forEach(function (part) { if (part) taken += part.offsetHeight + 6; });
    let rows = 0;
    let head = 0;
    grids.forEach(function (grid) {
      rows = Math.max(rows, rowsOf(grid));
      const title = grid.parentElement && grid.parentElement.querySelector('h2');
      if (title) head = Math.max(head, title.offsetHeight + 6);
    });
    if (rows === 0) return maxCell;
    const room = (window.innerHeight - taken - head - (rows - 1) * gap) / rows;
    return Math.max(minCell, Math.min(maxCell, Math.floor(room)));
  }

  function byWidth(grids) {
    const list = document.querySelector('.hb .decklist');
    if (!list) return maxCell;
    let columns = 0;
    grids.forEach(function (grid) { columns += colsOf(grid); });
    if (columns === 0) return maxCell;
    const between = 18 * (grids.length - 1) + gap * (columns - grids.length);
    return Math.max(minCell, Math.min(maxCell, Math.floor((list.clientWidth - between) / columns)));
  }

  function overflows(key) {
    const label = key.querySelector('.label');
    if (!label) return false;
    return label.scrollHeight > label.clientHeight + 1 || label.scrollWidth > label.clientWidth + 1;
  }

  function largest(key, most) {
    const label = key.querySelector('.label');
    let low = minFont;
    let high = most;
    label.style.fontSize = high + 'px';
    if (!overflows(key)) return high;
    while (high - low > 1) {
      const middle = Math.floor((low + high) / 2);
      label.style.fontSize = middle + 'px';
      if (overflows(key)) high = middle; else low = middle;
    }
    return low;
  }

  function fitGrid(grid) {
    const keys = Array.from(grid.querySelectorAll('.key')).filter(function (key) {
      const label = key.querySelector('.label');
      return label && label.textContent.trim().length > 0;
    });
    if (!keys.length) return;
    const sample = keys[0];
    const most = Math.max(minFont, Math.min(Math.round(sample.clientWidth / 5), Math.round(sample.clientHeight / 4)));
    const best = keys.map(function (key) { return largest(key, most); });
    const sorted = best.slice().sort(function (a, b) { return a - b; });
    const uniform = sorted[Math.floor(sorted.length / 2)];
    keys.forEach(function (key, index) {
      key.querySelector('.label').style.fontSize = Math.min(uniform, best[index]) + 'px';
    });
  }

  function signature(grids) {
    return innerWidth + 'x' + innerHeight + '|' + grids
      .map(function (grid) { return grid.innerText.replace(/\s+/g, ' '); })
      .join('#');
  }

  function layout() {
    if (frame) clearTimeout(frame);
    frame = setTimeout(function () {
      frame = 0;
      const grids = Array.from(document.querySelectorAll('.hb .grid'));
      if (!grids.length) return;
      const mark = signature(grids);
      if (mark === laid) return;
      laid = mark;
      const cell = Math.max(minCell, Math.min(byHeight(grids), byWidth(grids), maxCell));
      grids.forEach(function (grid) {
        const cols = colsOf(grid);
        grid.style.setProperty('--cell', cell + 'px');
        grid.style.width = (cell * cols + (cols - 1) * gap) + 'px';
      });
      grids.forEach(fitGrid);
    }, 120);
  }

  board.fit = layout;

  const observer = new MutationObserver(layout);

  window.hallBoard = board;
})();
