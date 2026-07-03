// Backoffice dashboard element for the "Ticketing" section.
// It is a bare custom element that embeds the Blazor admin app in an iframe,
// isolating it from the backoffice styles/DOM. Mirrors the Sporthalle pattern.
class TicketingAdminView extends HTMLElement {
  connectedCallback() {
    this.style.cssText = 'display:block;width:100%;height:100%;';
    const iframe = document.createElement('iframe');
    iframe.src = '/admin/ticketing';
    iframe.title = 'Ticketing Admin';
    iframe.style.cssText = 'width:100%;height:calc(100vh - 60px);border:none;display:block;';
    this.appendChild(iframe);
  }

  disconnectedCallback() {
    this.innerHTML = '';
  }
}

customElements.define('ticketing-admin-view', TicketingAdminView);
