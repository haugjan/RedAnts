// JS interop bridge between the Blazor ScanView component and the html5-qrcode library.
// Starts the rear camera, reports each decoded code to .NET, and pauses until resume() is called.
window.ticketScanner = (function () {
    let instance = null;
    let dotNetRef = null;
    let audioCtx = null;

    // ---- Feedback beeps (Web Audio, no audio files needed) ----
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

    // success: two short ascending sine beeps; failure: one low square buzz.
    function beep(success) {
        const ctx = ensureAudio();
        if (!ctx) return;
        if (success) {
            tone(ctx, 880, 0, 0.12, "sine");
            tone(ctx, 1320, 0.13, 0.18, "sine");
        } else {
            tone(ctx, 200, 0, 0.4, "square");
        }
    }

    async function start(elementId, ref) {
        dotNetRef = ref;

        if (typeof Html5Qrcode === "undefined") {
            throw new Error("html5-qrcode wurde nicht geladen.");
        }

        // Tear down a previous instance (e.g. after navigating back and forth).
        await stop();

        instance = new Html5Qrcode(elementId, /* verbose */ false);

        const config = {
            fps: 10,
            qrbox: { width: 250, height: 250 },
            aspectRatio: 1.0
        };

        const onScanSuccess = (decodedText) => {
            // Pause immediately so the same code is not reported repeatedly, then hand off to .NET.
            try { instance.pause(/* shouldPauseVideo */ true); } catch (e) { /* ignore */ }
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("OnCodeScanned", decodedText);
            }
        };

        // facingMode: "environment" selects the rear camera on phones.
        await instance.start({ facingMode: "environment" }, config, onScanSuccess, undefined);
    }

    function resume() {
        if (instance) {
            try { instance.resume(); } catch (e) { /* not paused */ }
        }
    }

    async function stop() {
        if (!instance) return;
        try {
            await instance.stop();
            instance.clear();
        } catch (e) {
            /* already stopped */
        }
        instance = null;
    }

    return { start, resume, stop, beep };
})();
