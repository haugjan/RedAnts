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
        gain.gain.exponentialRampToValueAtTime(0.3, t + 0.01);
        gain.gain.exponentialRampToValueAtTime(0.0001, t + duration);
        osc.start(t);
        osc.stop(t + duration + 0.02);
    }

    function beep(success) {
        const ctx = ensureAudio();
        if (!ctx) return;
        if (success) {
            tone(ctx, 988, 0, 0.10, "triangle");
            tone(ctx, 1319, 0.11, 0.10, "triangle");
            tone(ctx, 1760, 0.22, 0.30, "triangle");
        } else {
            tone(ctx, 200, 0, 0.4, "square");
        }
    }

    async function start(elementId, ref) {
        dotNetRef = ref;

        if (typeof Html5Qrcode === "undefined") {
            throw new Error("html5-qrcode wurde nicht geladen.");
        }

        await stop();

        instance = new Html5Qrcode(elementId, false);

        const config = {
            fps: 10,
            qrbox: { width: 250, height: 250 },
            aspectRatio: 1.0
        };

        const onScanSuccess = (decodedText) => {
            try { instance.pause(true); } catch (e) { }
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("OnCodeScanned", decodedText);
            }
        };

        await instance.start({ facingMode: "environment" }, config, onScanSuccess, undefined);
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
