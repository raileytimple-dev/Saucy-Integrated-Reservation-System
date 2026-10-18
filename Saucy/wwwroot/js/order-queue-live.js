(() => {
    const sync = document.querySelector('[data-live-order-queue]');
    if (!sync) return;

    let version = sync.dataset.version ?? '';
    let timer;
    const endpoint = sync.dataset.endpoint;
    const message = sync.querySelector('[data-live-message]');
    const refresh = sync.querySelector('[data-live-refresh]');
    const userIsEditing = () => Boolean(document.querySelector('input:focus, select:focus, textarea:focus, details[open]'));

    refresh?.addEventListener('click', () => window.location.reload());

    async function checkQueue() {
        window.clearTimeout(timer);
        try {
            const response = await fetch(endpoint, {
                cache: 'no-store',
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) throw new Error('Queue check failed.');

            const result = await response.json();
            if (result.version !== version) {
                if (userIsEditing()) {
                    version = result.version;
                    message.textContent = 'New order activity is available';
                    refresh.hidden = false;
                    sync.classList.add('has-update');
                } else {
                    window.location.reload();
                    return;
                }
            } else if (refresh?.hidden) {
                message.textContent = `Live · checked ${new Date().toLocaleTimeString([], {
                    hour: 'numeric', minute: '2-digit', second: '2-digit'
                })}`;
                sync.classList.remove('is-offline');
            }
        } catch {
            message.textContent = 'Live sync reconnecting…';
            sync.classList.add('is-offline');
        }

        timer = window.setTimeout(checkQueue, document.hidden ? 5000 : 2000);
    }

    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) checkQueue();
    });
    timer = window.setTimeout(checkQueue, 1000);
})();
