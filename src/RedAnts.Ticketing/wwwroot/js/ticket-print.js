(function () {
    const PT_PER_MM = 72 / 25.4;
    const QUIET_MM = 2;
    const PDFJS_BASE = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@4.7.76/build/';
    let pdfjs = null;
    let pageObj = null;
    let baseWidthPt = 0;
    let baseScale = 1;
    let zoom = 1;

    function $(id) { return document.getElementById(id); }

    async function loadPdfjs() {
        if (pdfjs) return pdfjs;
        const mod = await import(PDFJS_BASE + 'pdf.min.mjs');
        mod.GlobalWorkerOptions.workerSrc = PDFJS_BASE + 'pdf.worker.min.mjs';
        pdfjs = mod;
        return mod;
    }

    function num(id, fallback) {
        const el = $(id);
        const v = el ? parseFloat(el.value) : NaN;
        return isFinite(v) ? v : fallback;
    }

    function set(id, v) {
        const el = $(id);
        if (el) el.value = Math.round(v * 10) / 10;
    }

    function layout() {
        return {
            pageW: num('fpPageW', 91),
            pageH: num('fpPageH', 61),
            qrX: num('fpQrX', 5),
            qrY: num('fpQrY', 5),
            qrSize: num('fpQrSize', 25),
            fontPt: num('fpFontPt', 8),
            nameX: num('fpNameX', 34),
            nameY: num('fpNameY', 8),
            nameFontPt: num('fpNameFontPt', 9),
            nameMaxW: num('fpNameMaxW', 52)
        };
    }

    function showName() {
        const el = $('fpShowName');
        return !!(el && el.checked);
    }

    function pxPerMm() {
        const canvas = $('fpCanvas');
        const w = canvas ? canvas.clientWidth : 0;
        return w > 0 ? w / layout().pageW : 0;
    }

    function clamp() {
        const l = layout();
        set('fpQrSize', Math.max(5, Math.min(l.qrSize, l.pageW, l.pageH)));
        const s = num('fpQrSize', l.qrSize);
        set('fpQrX', Math.max(0, Math.min(l.qrX, l.pageW - s)));
        set('fpQrY', Math.max(0, Math.min(l.qrY, l.pageH - s)));
        set('fpNameMaxW', Math.max(5, Math.min(l.nameMaxW, l.pageW)));
        const nw = num('fpNameMaxW', l.nameMaxW);
        set('fpNameX', Math.max(0, Math.min(l.nameX, l.pageW - nw)));
        set('fpNameY', Math.max(0, Math.min(l.nameY, l.pageH)));
    }

    function place() {
        const box = $('fpQrBox');
        const k = pxPerMm();
        if (!box || k <= 0) return;
        const l = layout();
        box.style.left = (l.qrX * k) + 'px';
        box.style.top = (l.qrY * k) + 'px';
        box.style.width = (l.qrSize * k) + 'px';
        box.style.height = (l.qrSize * k) + 'px';
        box.style.boxShadow = '0 0 0 ' + (QUIET_MM * k) + 'px #fff';

        const label = $('fpCodeLabel');
        if (label) {
            label.style.display = 'block';
            label.style.left = (l.qrX * k) + 'px';
            label.style.top = ((l.qrY + l.qrSize) * k + l.fontPt * 0.4 * pxPerPt(k)) + 'px';
            label.style.width = (l.qrSize * k) + 'px';
            label.style.fontSize = (l.fontPt * pxPerPt(k)) + 'px';
        }

        placeName(l, k);
        drawMarks(l, k);
    }

    function placeName(l, k) {
        const nameBox = $('fpNameBox');
        if (!nameBox) return;
        if (!showName()) { nameBox.style.display = 'none'; return; }
        nameBox.style.display = 'flex';
        nameBox.style.left = (l.nameX * k) + 'px';
        nameBox.style.top = (l.nameY * k) + 'px';
        nameBox.style.width = (l.nameMaxW * k) + 'px';
        nameBox.style.height = (l.nameFontPt * 1.6 * pxPerPt(k)) + 'px';
        nameBox.style.fontSize = (l.nameFontPt * pxPerPt(k)) + 'px';
        var align = num('fpNameAlign', 0);
        nameBox.style.justifyContent = align === 2 ? 'flex-end' : align === 1 ? 'center' : 'flex-start';
    }

    function drawMarks(l, k) {
        const svg = $('fpMarks');
        const canvas = $('fpCanvas');
        if (!svg || !canvas) return;
        const w = canvas.clientWidth, h = canvas.clientHeight;
        svg.setAttribute('width', w);
        svg.setAttribute('height', h);
        svg.setAttribute('overflow', 'visible');
        while (svg.firstChild) svg.removeChild(svg.firstChild);
        const NS = 'http://www.w3.org/2000/svg';
        const arm = 12, gap = 4;
        const corners = [
            [(l.qrX - QUIET_MM) * k, (l.qrY - QUIET_MM) * k],
            [(l.qrX + l.qrSize + QUIET_MM) * k, (l.qrY - QUIET_MM) * k],
            [(l.qrX - QUIET_MM) * k, (l.qrY + l.qrSize + QUIET_MM) * k],
            [(l.qrX + l.qrSize + QUIET_MM) * k, (l.qrY + l.qrSize + QUIET_MM) * k]
        ];
        corners.forEach(function (c) {
            const cx = c[0], cy = c[1];
            addSvgLine(svg, NS, cx - arm, cy, cx - gap, cy);
            addSvgLine(svg, NS, cx + gap, cy, cx + arm, cy);
            addSvgLine(svg, NS, cx, cy - arm, cx, cy - gap);
            addSvgLine(svg, NS, cx, cy + gap, cx, cy + arm);
        });
    }

    function addSvgLine(svg, NS, x1, y1, x2, y2) {
        var bg = document.createElementNS(NS, 'line');
        bg.setAttribute('x1', x1); bg.setAttribute('y1', y1);
        bg.setAttribute('x2', x2); bg.setAttribute('y2', y2);
        bg.setAttribute('stroke', '#fff'); bg.setAttribute('stroke-width', '4');
        bg.setAttribute('stroke-linecap', 'round');
        svg.appendChild(bg);
        var fg = document.createElementNS(NS, 'line');
        fg.setAttribute('x1', x1); fg.setAttribute('y1', y1);
        fg.setAttribute('x2', x2); fg.setAttribute('y2', y2);
        fg.setAttribute('stroke', '#e53'); fg.setAttribute('stroke-width', '2');
        fg.setAttribute('stroke-linecap', 'round');
        svg.appendChild(fg);
    }

    function pxPerPt(k) {
        return k / PT_PER_MM;
    }

    function cssSize() {
        const canvas = $('fpCanvas');
        if (!canvas || !baseWidthPt) return;
        canvas.style.width = (baseWidthPt * baseScale * zoom) + 'px';
        canvas.style.height = 'auto';
    }

    async function draw() {
        if (!pageObj) return;
        const dpr = window.devicePixelRatio || 1;
        const display = baseScale * zoom;
        const vp = pageObj.getViewport({ scale: display * dpr });
        const canvas = $('fpCanvas');
        canvas.width = vp.width;
        canvas.height = vp.height;
        canvas.style.width = (baseWidthPt * display) + 'px';
        canvas.style.height = 'auto';
        await pageObj.render({ canvasContext: canvas.getContext('2d'), viewport: vp }).promise;
        place();
    }

    async function render(file) {
        const lib = await loadPdfjs();
        const buf = await file.arrayBuffer();
        const doc = await lib.getDocument({ data: buf }).promise;
        pageObj = await doc.getPage(1);
        const base = pageObj.getViewport({ scale: 1 });
        baseWidthPt = base.width;

        set('fpPageW', base.width / PT_PER_MM);
        set('fpPageH', base.height / PT_PER_MM);

        const stage = $('fpStage');
        const target = (stage ? stage.clientWidth : 0) || 500;
        baseScale = target / base.width;
        zoom = 1;
        const zoomEl = $('fpZoom');
        if (zoomEl) zoomEl.value = 100;
        const zoomVal = $('fpZoomVal');
        if (zoomVal) zoomVal.textContent = '100%';

        const box = $('fpQrBox');
        if (box) box.style.display = 'block';
        clamp();
        await draw();
        const submit = $('fpSubmit');
        if (submit) submit.disabled = false;
    }

    function applyZoom(pct, rerender) {
        zoom = Math.max(1, Math.min(10, pct / 100));
        const zoomVal = $('fpZoomVal');
        if (zoomVal) zoomVal.textContent = Math.round(zoom * 100) + '%';
        if (rerender) { draw(); } else { cssSize(); place(); }
    }

    function drag(e) {
        const isHandleBR = e.target.classList.contains('fp-handle');
        const isHandleTL = e.target.classList.contains('fp-handle-tl');
        const k = pxPerMm();
        if (k <= 0) return;
        const sx = e.clientX, sy = e.clientY;
        const l0 = layout();
        e.preventDefault();
        e.stopPropagation();
        function move(ev) {
            const dx = (ev.clientX - sx) / k;
            const dy = (ev.clientY - sy) / k;
            if (isHandleBR) {
                set('fpQrSize', l0.qrSize + dx);
            } else if (isHandleTL) {
                const delta = (dx + dy) / 2;
                set('fpQrX', l0.qrX + delta);
                set('fpQrY', l0.qrY + delta);
                set('fpQrSize', l0.qrSize - delta);
            } else {
                set('fpQrX', l0.qrX + dx);
                set('fpQrY', l0.qrY + dy);
            }
            clamp();
            place();
        }
        function up() {
            document.removeEventListener('pointermove', move);
            document.removeEventListener('pointerup', up);
        }
        document.addEventListener('pointermove', move);
        document.addEventListener('pointerup', up);
    }

    function dragName(e) {
        const isHandle = e.target.classList.contains('fp-name-handle');
        const k = pxPerMm();
        if (k <= 0) return;
        const sx = e.clientX, sy = e.clientY;
        const l0 = layout();
        e.preventDefault();
        e.stopPropagation();
        function move(ev) {
            const dx = (ev.clientX - sx) / k;
            const dy = (ev.clientY - sy) / k;
            if (isHandle) {
                set('fpNameMaxW', l0.nameMaxW + dx);
            } else {
                set('fpNameX', l0.nameX + dx);
                set('fpNameY', l0.nameY + dy);
            }
            clamp();
            place();
        }
        function up() {
            document.removeEventListener('pointermove', move);
            document.removeEventListener('pointerup', up);
        }
        document.addEventListener('pointermove', move);
        document.addEventListener('pointerup', up);
    }

    window.ticketPrint = {
        init() {
            pageObj = null;
            zoom = 1;
            const file = $('fpFile');
            if (file && !file._fp) {
                file._fp = true;
                file.addEventListener('change', e => {
                    const f = e.target.files && e.target.files[0];
                    if (f) render(f);
                });
            }
            const box = $('fpQrBox');
            if (box && !box._fp) {
                box._fp = true;
                box.addEventListener('pointerdown', drag);
            }
            const nameBox = $('fpNameBox');
            if (nameBox && !nameBox._fp) {
                nameBox._fp = true;
                nameBox.addEventListener('pointerdown', dragName);
            }
            ['fpQrX', 'fpQrY', 'fpQrSize', 'fpPageW', 'fpPageH', 'fpFontPt',
             'fpNameX', 'fpNameY', 'fpNameFontPt', 'fpNameMaxW'].forEach(id => {
                const el = $(id);
                if (el && !el._fp) {
                    el._fp = true;
                    el.addEventListener('input', () => { clamp(); place(); });
                }
            });
            const showEl = $('fpShowName');
            if (showEl && !showEl._fp) {
                showEl._fp = true;
                showEl.addEventListener('change', place);
            }
            const alignEl = $('fpNameAlign');
            if (alignEl && !alignEl._fp) {
                alignEl._fp = true;
                alignEl.addEventListener('change', place);
            }
            const zoomEl = $('fpZoom');
            if (zoomEl && !zoomEl._fp) {
                zoomEl._fp = true;
                zoomEl.addEventListener('input', () => applyZoom(parseFloat(zoomEl.value), false));
                zoomEl.addEventListener('change', () => applyZoom(parseFloat(zoomEl.value), true));
            }
            const submit = $('fpSubmit');
            if (submit) submit.disabled = true;
        }
    };
})();
