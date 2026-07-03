// Backoffice dashboard elements for the "Ticketing" section — one custom element per admin tab.
// Each is a bare custom element that embeds the Blazor admin app in an iframe (isolating it from the
// backoffice styles/DOM), preselecting its tab via the ?tab= query. There is one element per tab so
// each native Umbraco dashboard renders a fresh iframe for its tab, which keeps deep links
// (/umbraco/section/ticketing/dashboard/{tab}) and tab switches in sync with the browser URL.
// Mirrors the Sporthalle pattern (iframe-embedded Blazor), extended for per-tab dashboards.

function defineTabElement(tag, tab) {
  if (customElements.get(tag)) return;
  customElements.define(tag, class extends HTMLElement {
    connectedCallback() {
      // Explicit viewport-based height: in the multi-dashboard tab layout the host panel is
      // auto-height, so height:100% would collapse to 0 and hide the iframe. Offset ~110px covers
      // the Umbraco section nav plus the dashboard tab bar.
      this.style.cssText = 'display:block;width:100%;height:calc(100vh - 110px);';
      const iframe = document.createElement('iframe');
      iframe.src = '/admin/ticketing?tab=' + tab;
      iframe.title = 'Ticketing Admin';
      iframe.style.cssText = 'width:100%;height:100%;border:none;display:block;';
      this.appendChild(iframe);
    }

    disconnectedCallback() {
      this.innerHTML = '';
    }
  });
}

defineTabElement('ticketing-admin-events', 'events');
defineTabElement('ticketing-admin-tickets', 'tickets');
defineTabElement('ticketing-admin-seasoncards', 'seasoncards');
defineTabElement('ticketing-admin-membercards', 'membercards');
