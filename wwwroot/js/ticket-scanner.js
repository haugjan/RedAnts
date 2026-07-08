window.ticketScanner = (function () {
    let scanner = null;
    let video = null;
    let dotNetRef = null;
    let paused = false;
    let audioCtx = null;

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

    async function applyHd() {
        const stream = video && video.srcObject;
        if (!stream) return;
        const track = stream.getVideoTracks()[0];
        if (!track) return;
        try {
            await track.applyConstraints({
                width: { ideal: 1280 },
                height: { ideal: 720 },
                facingMode: "environment"
            });
        } catch (e) { }
    }

    async function start(elementId, ref) {
        dotNetRef = ref;
        paused = false;

        if (typeof QrScanner === "undefined") {
            throw new Error("qr-scanner wurde nicht geladen.");
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

        scanner = new QrScanner(
            video,
            (result) => {
                if (paused) return;
                paused = true;
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync("OnCodeScanned", result.data);
                }
            },
            {
                preferredCamera: "environment",
                maxScansPerSecond: 25,
                highlightScanRegion: false,
                highlightCodeOutline: false,
                returnDetailedScanResult: true,
                calculateScanRegion: (v) => ({
                    x: 0, y: 0, width: v.videoWidth, height: v.videoHeight
                })
            }
        );

        await scanner.start();
        await applyHd();
    }

    function resume() {
        paused = false;
    }

    async function stop() {
        paused = true;
        if (scanner) {
            try { scanner.stop(); } catch (e) { }
            try { scanner.destroy(); } catch (e) { }
            scanner = null;
        }
        if (video && video.parentNode) {
            video.parentNode.removeChild(video);
        }
        video = null;
    }

    return { start, resume, stop, beep };
})();
