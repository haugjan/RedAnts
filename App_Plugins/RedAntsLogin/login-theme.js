document.querySelectorAll('link[rel*="icon"]').forEach(el => el.remove());
const favicon = document.createElement('link');
favicon.rel = 'icon';
favicon.href = '/favicons/favicon.ico';
document.head.appendChild(favicon);
