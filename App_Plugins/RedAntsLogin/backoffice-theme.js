function applyFavicon() {
  document.querySelectorAll('link[rel*="icon"]').forEach(el => {
    if (!el.getAttribute('href')?.includes('/favicons/favicon.ico')) el.remove();
  });
  if (!document.querySelector('link[href="/favicons/favicon.ico"]')) {
    const link = document.createElement('link');
    link.rel = 'icon';
    link.href = '/favicons/favicon.ico';
    document.head.appendChild(link);
  }
}

applyFavicon();

new MutationObserver(records => {
  let needsUpdate = false;
  records.forEach(r => r.addedNodes.forEach(n => {
    if (n.nodeName === 'LINK' && n.rel?.includes?.('icon') &&
        !n.getAttribute('href')?.includes('/favicons/favicon.ico')) {
      n.remove();
      needsUpdate = true;
    }
  }));
  if (needsUpdate) applyFavicon();
}).observe(document.head, { childList: true });
