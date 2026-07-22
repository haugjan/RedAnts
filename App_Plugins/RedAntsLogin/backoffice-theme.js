const style = document.createElement('style');
style.textContent = `
  :root {
    --uui-color-interactive: #a01828;
    --uui-color-interactive-emphasis: #7e1220;
    --uui-color-interactive-standalone: #c44050;
    --uui-color-interactive-contrast: #ffffff;
    --uui-color-focus: #a01828;
    --uui-color-current: #a01828;
    --uui-color-current-emphasis: #7e1220;
    --uui-color-current-standalone: #c44050;
    --uui-color-current-contrast: #ffffff;
    --uui-color-selected: rgba(160,24,40,0.15);
    --uui-color-selected-emphasis: rgba(160,24,40,0.25);
    --uui-color-selected-contrast: #a01828;
    --uui-color-default: #a01828;
    --uui-color-default-emphasis: #7e1220;
    --uui-color-default-standalone: #c44050;
    --uui-color-default-contrast: #ffffff;
    --uui-color-header: #4a1018;
    --uui-color-header-contrast: #ffffff;
    --uui-color-header-surface: #3d0e16;
    --uui-color-header-surface-contrast: rgba(255,255,255,0.75);
  }

  uui-button,
  uui-tab,
  uui-menu-item,
  uui-checkbox,
  uui-toggle,
  uui-radio,
  uui-select,
  uui-input,
  uui-textarea,
  uui-range-slider,
  uui-pagination,
  uui-ref-node,
  uui-combobox,
  uui-badge {
    --uui-color-interactive: #a01828;
    --uui-color-interactive-emphasis: #7e1220;
    --uui-color-interactive-standalone: #c44050;
    --uui-color-interactive-contrast: #ffffff;
    --uui-color-focus: #a01828;
    --uui-color-current: #a01828;
    --uui-color-current-emphasis: #7e1220;
    --uui-color-current-contrast: #ffffff;
    --uui-color-selected: rgba(160,24,40,0.15);
    --uui-color-default: #a01828;
    --uui-color-default-emphasis: #7e1220;
    --uui-color-default-contrast: #ffffff;
  }

  uui-sidebar,
  uui-sidebar-tabs,
  umb-backoffice-sidebar,
  umb-section-sidebar,
  umb-sidebar,
  umb-sidebar-context {
    --uui-color-header: #4a1018;
    --uui-color-header-contrast: #ffffff;
    --uui-color-header-surface: #3d0e16;
    --uui-color-header-surface-contrast: rgba(255,255,255,0.75);
    --uui-color-interactive: #d02d38;
    --uui-color-interactive-emphasis: #a01828;
    --uui-color-current: #d02d38;
    --uui-color-current-emphasis: #a01828;
    --uui-color-selected: rgba(255,255,255,0.12);
  }
`;
document.head.appendChild(style);
