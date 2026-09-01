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

(function addSwitchAccountLink() {
  if (document.getElementById('ra-ms-signout')) return;
  const logoutUrl = 'https://login.microsoftonline.com/common/oauth2/v2.0/logout'
    + '?post_logout_redirect_uri=' + encodeURIComponent(location.origin + '/umbraco/login');
  const a = document.createElement('a');
  a.id = 'ra-ms-signout';
  a.href = logoutUrl;
  a.textContent = 'Anderes Microsoft-Konto verwenden';
  a.style.cssText = 'position:fixed;bottom:1rem;right:1rem;font-size:0.75rem;'
    + 'color:#888;opacity:0.7;text-decoration:underline;z-index:9999;';
  document.body.appendChild(a);
})();

new MutationObserver(records => {
  let needsUpdate = false;
  records.forEach(r => {
    if (r.type === 'childList') {
      r.addedNodes.forEach(n => {
        if (n.nodeName === 'LINK' && n.rel?.includes?.('icon') &&
            !n.getAttribute('href')?.includes('/favicons/favicon.ico')) {
          needsUpdate = true;
        }
      });
    } else if (r.type === 'attributes') {
      const n = r.target;
      if (n.nodeName === 'LINK' && n.rel?.includes?.('icon') &&
          !n.getAttribute('href')?.includes('/favicons/favicon.ico')) {
        needsUpdate = true;
      }
    }
  });
  if (needsUpdate) applyFavicon();
}).observe(document.head, { childList: true, subtree: true, attributes: true, attributeFilter: ['href', 'rel'] });
