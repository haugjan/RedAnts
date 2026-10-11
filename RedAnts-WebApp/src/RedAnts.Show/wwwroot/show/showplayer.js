window.showPlayer = {
    play(element, url, startSec, durationSec, volume, loop) {
        if (!element) return;
        element.pause();
        element.src = url;
        element.volume = Math.min(1, Math.max(0, volume ?? 1));
        element.loop = !!loop;
        element.currentTime = startSec || 0;
        element.onloadedmetadata = () => { element.currentTime = startSec || 0; };
        element.ontimeupdate = () => {
            if (!durationSec) return;
            if (element.currentTime >= (startSec || 0) + durationSec) {
                if (element.loop) element.currentTime = startSec || 0;
                else element.pause();
            }
        };
        element.play().catch(() => { });
    },
    stop(element) {
        if (!element) return;
        element.pause();
        element.removeAttribute('src');
        element.load();
    },
};
