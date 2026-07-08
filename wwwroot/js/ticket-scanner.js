window.ticketScanner = (function () {
    let stream = null;
    let video = null;
    let canvas = null;
    let ctx = null;
    let dotNetRef = null;
    let paused = false;
    let rafId = null;
    let lastScan = 0;
    let heldCode = null;
    let heldSeenAt = 0;
    let audioCtx = null;

    const RearmMs = 1500;

    function ensureAudio() {
        if (!audioCtx) {
            const AC = window.AudioContext || window.webkitAudioContext;
            if (!AC) return null;
            audioCtx = new AC();
        }
        if (audioCtx.state === "suspended") {
            audioCtx.resume();
        }
        return audioCtx;
    }

    function tone(ctx, freq, startOffset, duration, type) {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = type;
        osc.frequency.value = freq;
        osc.connect(gain);
        gain.connect(ctx.destination);
        const t = ctx.currentTime + startOffset;
        gain.gain.setValueAtTime(0.0001, t);
        gain.gain.exponentialRampToValueAtTime(0.6, t + 0.01);
        gain.gain.exponentialRampToValueAtTime(0.0001, t + duration);
        osc.start(t);
        osc.stop(t + duration + 0.02);
    }

    function beep(success) {
        const ctx = ensureAudio();
        if (!ctx) return;
        if (success) {
            tone(ctx, 1568, 0, 0.35, "triangle");
        } else {
            tone(ctx, 220, 0, 0.18, "square");
            tone(ctx, 220, 0.28, 0.18, "square");
        }
    }

    function scanFrame() {
        rafId = requestAnimationFrame(scanFrame);
        if (!video || video.readyState < 2) return;

        const now = performance.now();
        if (now - lastScan < 120) return;
        lastScan = now;

        const w = video.videoWidth;
        const h = video.videoHeight;
        if (!w || !h) return;
        if (canvas.width !== w) canvas.width = w;
        if (canvas.height !== h) canvas.height = h;
        ctx.drawImage(video, 0, 0, w, h);

        let image;
        try {
            image = ctx.getImageData(0, 0, w, h);
        } catch (e) {
            return;
        }

        const code = jsQR(image.data, w, h, { inversionAttempts: "dontInvert" });
        if (code && code.data) {
            if (code.data === heldCode) {
                heldSeenAt = now;
                return;
            }
            if (paused) return;
            heldCode = code.data;
            heldSeenAt = now;
            paused = true;
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("OnCodeScanned", code.data);
            }
        } else if (heldCode && now - heldSeenAt > RearmMs) {
            heldCode = null;
        }
    }

    async function start(elementId, ref) {
        dotNetRef = ref;
        paused = false;

        if (typeof jsQR === "undefined") {
            throw new Error("QR-Dekoder wurde nicht geladen.");
        }

        await stop();

        const container = document.getElementById(elementId);
        if (!container) {
            throw new Error("Scanner-Element wurde nicht gefunden.");
        }
        container.innerHTML = "";

        video = document.createElement("video");
        video.setAttribute("playsinline", "");
        video.setAttribute("muted", "");
        video.muted = true;
        video.style.width = "100%";
        container.appendChild(video);

        try {
            stream = await navigator.mediaDevices.getUserMedia({
                audio: false,
                video: {
                    facingMode: { ideal: "environment" },
                    width: { ideal: 1280 },
                    height: { ideal: 720 }
                }
            });
        } catch (e) {
            throw new Error("Kamerazugriff nicht möglich: " + (e && e.message ? e.message : e));
        }

        video.srcObject = stream;
        await video.play();

        canvas = document.createElement("canvas");
        ctx = canvas.getContext("2d", { willReadFrequently: true });

        paused = false;
        lastScan = 0;
        heldCode = null;
        heldSeenAt = 0;
        scanFrame();
    }

    function resume() {
        paused = false;
    }

    async function stop() {
        paused = true;
        if (rafId) {
            cancelAnimationFrame(rafId);
            rafId = null;
        }
        if (stream) {
            stream.getTracks().forEach((t) => { try { t.stop(); } catch (e) { } });
            stream = null;
        }
        if (video) {
            try { video.pause(); } catch (e) { }
            video.srcObject = null;
            if (video.parentNode) video.parentNode.removeChild(video);
            video = null;
        }
        canvas = null;
        ctx = null;
    }

    return { start, resume, stop, beep };
})();
