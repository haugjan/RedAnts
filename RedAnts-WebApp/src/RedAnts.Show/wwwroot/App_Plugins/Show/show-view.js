const showPages = ['hallen', 'musik', 'decks', 'bedienung', 'vmix', 'led'];

class ShowAdminView extends HTMLElement {
  connectedCallback() {
    this.style.cssText = 'display:block;width:100%;height:100%;';
    const last = (location.pathname.split('/').filter(Boolean).pop() || '').toLowerCase();
    const page = showPages.includes(last) ? last : 'hallen';
    const iframe = document.createElement('iframe');
    iframe.src = '/admin/show/' + page;
    iframe.title = 'Show';
    iframe.allow = 'clipboard-read; clipboard-write';
    iframe.style.cssText = 'width:100%;height:calc(100vh - 60px);border:none;display:block;';
    this.appendChild(iframe);
  }
  disconnectedCallback() { this.innerHTML = ''; }
}
customElements.define('show-admin-view', ShowAdminView);
