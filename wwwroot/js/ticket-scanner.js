// JS interop bridge between the Blazor ScanView component and the html5-qrcode library.
// Starts the rear camera, reports each decoded code to .NET, and pauses until resume() is called.
window.ticketScanner = (function () {
    let instance = null;
    let dotNetRef = null;

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

    return { start, resume, stop };
})();
