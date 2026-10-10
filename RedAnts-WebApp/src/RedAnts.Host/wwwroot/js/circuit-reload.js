(function () {
    var overlay = null;

    function show() {
        if (overlay) return;
        overlay = document.createElement('div');
        overlay.id = 'ra-circuit-overlay';
        overlay.setAttribute('role', 'status');
        overlay.style.cssText = 'position:fixed;left:0;right:0;bottom:0;z-index:2147483647;background:#C8102E;color:#fff;font:600 15px/1.4 Verdana,Geneva,sans-serif;text-align:center;padding:12px 16px;box-shadow:0 -2px 8px rgba(0,0,0,.25)';
        overlay.textContent = 'Verbindung zum Server unterbrochen. Die Seite lädt automatisch neu, sobald er wieder antwortet.';
        document.body.appendChild(overlay);
    }

    function hide() {
        if (!overlay) return;
        overlay.remove();
        overlay = null;
    }

    function sleep(ms) {
        return new Promise(function (resolve) { setTimeout(resolve, ms); });
    }

    function mediaPlaying() {
        if (typeof window.redants.mediaPlaying === 'function') return window.redants.mediaPlaying();
        var elements = document.querySelectorAll('audio,video');
        for (var i = 0; i < elements.length; i++) {
            if (!elements[i].paused && !elements[i].ended) return true;
        }
        return false;
    }

    async function reloadWhenServerIsBack() {
        for (;;) {
            await sleep(2000);
            var reconnected;
            try { reconnected = await Blazor.reconnect(); }
            catch (e) { reconnected = null; }
            if (reconnected === true) { hide(); return; }
            if (reconnected === false) {
                while (mediaPlaying()) await sleep(1000);
                location.reload();
                return;
            }
        }
    }

    var handler = {
        onConnectionDown: function () { show(); reloadWhenServerIsBack(); },
        onConnectionUp: function () { hide(); }
    };

    window.redants = window.redants || {};
    window.redants.startCircuit = function (options) {
        return Blazor.start(Object.assign({ reconnectionHandler: handler }, options || {}));
    };
})();
