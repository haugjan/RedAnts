window.ticketScanner = (function () {
    let instance = null;
    let dotNetRef = null;
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

    async function start(elementId, ref) {
        dotNetRef = ref;

        if (typeof Html5Qrcode === "undefined") {
            throw new Error("html5-qrcode wurde nicht geladen.");
        }

        await stop();

        const qrOnly = typeof Html5QrcodeSupportedFormats !== "undefined"
            ? [Html5QrcodeSupportedFormats.QR_CODE]
            : undefined;

        instance = new Html5Qrcode(elementId, {
            verbose: false,
            formatsToSupport: qrOnly,
            experimentalFeatures: { useBarCodeDetectorIfSupported: true }
        });

        const config = {
            fps: 15,
            qrbox: (viewfinderWidth, viewfinderHeight) => {
                const edge = Math.floor(Math.min(viewfinderWidth, viewfinderHeight) * 0.8);
                return { width: edge, height: edge };
            }
        };

        const onScanSuccess = (decodedText) => {
            try { instance.pause(true); } catch (e) { }
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("OnCodeScanned", decodedText);
            }
        };

        const cameraConstraints = {
            facingMode: { ideal: "environment" },
            width: { ideal: 1280 },
            height: { ideal: 720 }
        };

        await instance.start(cameraConstraints, config, onScanSuccess, undefined);
    }

    function resume() {
        if (instance) {
            try { instance.resume(); } catch (e) { }
        }
    }

    async function stop() {
        if (!instance) return;
        try {
            await instance.stop();
            instance.clear();
        } catch (e) {
        }
        instance = null;
    }

    return { start, resume, stop, beep };
})();
