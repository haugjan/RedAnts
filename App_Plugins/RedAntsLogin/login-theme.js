const BRAND_CSS = `
  :root {
    --umb-login-primary-color: #a01828;
    --umb-login-curves-color: #a01828;
    --uui-color-interactive: #a01828;
    --uui-color-interactive-emphasis: #7e1220;
    --uui-color-interactive-contrast: #ffffff;
    --uui-color-default: #a01828;
    --uui-color-default-emphasis: #7e1220;
    --uui-color-default-contrast: #ffffff;
    --uui-color-focus: #a01828;
  }
  umb-auth-layout, umb-login {
    --uui-color-interactive: #a01828;
    --uui-color-interactive-emphasis: #7e1220;
    --uui-color-default: #a01828;
    --uui-color-default-emphasis: #7e1220;
    --uui-color-focus: #a01828;
  }
`;

const SHADOW_CSS = `
  :host {
    --uui-color-interactive: #a01828 !important;
    --uui-color-interactive-emphasis: #7e1220 !important;
    --uui-color-interactive-contrast: #ffffff !important;
    --uui-color-default: #a01828 !important;
    --uui-color-default-emphasis: #7e1220 !important;
    --uui-color-default-contrast: #ffffff !important;
    --uui-color-focus: #a01828 !important;
    --uui-color-current: #a01828 !important;
  }
`;

const rootStyle = document.createElement('style');
rootStyle.textContent = BRAND_CSS;
document.head.appendChild(rootStyle);

function brandShadowRoot(root) {
  if (root._ra) return;
  root._ra = true;
  const s = document.createElement('style');
  s.textContent = SHADOW_CSS;
  root.appendChild(s);
  root.querySelectorAll('*').forEach(el => {
    if (el.shadowRoot) brandShadowRoot(el.shadowRoot);
  });
}

new MutationObserver(records => {
  records.forEach(r => r.addedNodes.forEach(n => {
    if (n.nodeType !== 1) return;
    if (n.shadowRoot) brandShadowRoot(n.shadowRoot);
    n.querySelectorAll('*').forEach(el => {
      if (el.shadowRoot) brandShadowRoot(el.shadowRoot);
    });
  }));
}).observe(document.documentElement, { childList: true, subtree: true });

document.querySelectorAll('*').forEach(el => {
  if (el.shadowRoot) brandShadowRoot(el.shadowRoot);
});

document.querySelectorAll('link[rel*="icon"]').forEach(el => el.remove());
const favicon = document.createElement('link');
favicon.rel = 'icon';
favicon.href = '/favicons/favicon.ico';
document.head.appendChild(favicon);
