(function () {
    const PT_PER_MM = 72 / 25.4;
    const PDFJS_BASE = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@4.7.76/build/';
    let pdfjs = null;

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
            qrSize: num('fpQrSize', 25)
        };
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
    }

    async function render(file) {
        const lib = await loadPdfjs();
        const buf = await file.arrayBuffer();
        const doc = await lib.getDocument({ data: buf }).promise;
        const page = await doc.getPage(1);
        const base = page.getViewport({ scale: 1 });

        set('fpPageW', base.width / PT_PER_MM);
        set('fpPageH', base.height / PT_PER_MM);

        const stage = $('fpStage');
        const target = (stage ? stage.clientWidth : 0) || 500;
        const scale = target / base.width;
        const dpr = window.devicePixelRatio || 1;
        const vp = page.getViewport({ scale: scale * dpr });

        const canvas = $('fpCanvas');
        canvas.width = vp.width;
        canvas.height = vp.height;
        canvas.style.width = (base.width * scale) + 'px';
        canvas.style.height = (base.height * scale) + 'px';
        await page.render({ canvasContext: canvas.getContext('2d'), viewport: vp }).promise;

        const box = $('fpQrBox');
        if (box) box.style.display = 'block';
        clamp();
        place();
        const submit = $('fpSubmit');
        if (submit) submit.disabled = false;
    }

    function drag(e) {
        const isHandle = e.target.classList.contains('fp-handle');
        const k = pxPerMm();
        if (k <= 0) return;
        const sx = e.clientX, sy = e.clientY;
        const l0 = layout();
        e.preventDefault();
        function move(ev) {
            const dx = (ev.clientX - sx) / k;
            const dy = (ev.clientY - sy) / k;
            if (isHandle) {
                set('fpQrSize', l0.qrSize + dx);
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

    window.flexPrint = {
        init() {
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
            ['fpQrX', 'fpQrY', 'fpQrSize', 'fpPageW', 'fpPageH'].forEach(id => {
                const el = $(id);
                if (el && !el._fp) {
                    el._fp = true;
                    el.addEventListener('input', () => { clamp(); place(); });
                }
            });
            const submit = $('fpSubmit');
            if (submit) submit.disabled = true;
        }
    };
})();
