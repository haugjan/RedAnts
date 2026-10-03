Blazor.start({
    circuit: {
        reconnectionOptions: { maxRetries: 1000000, retryIntervalMilliseconds: 2000 }
    }
});

const reconnectModal = document.getElementById('components-reconnect-modal');
new MutationObserver(() => {
    if (reconnectModal.classList.contains('components-reconnect-rejected')
        || reconnectModal.classList.contains('components-reconnect-failed')) {
        setTimeout(() => location.reload(), 2000);
    }
}).observe(reconnectModal, { attributes: true, attributeFilter: ['class'] });
