const style = document.createElement('style');
style.textContent = `
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

  uui-button {
    --uui-color-interactive: #a01828;
    --uui-color-interactive-emphasis: #7e1220;
    --uui-color-interactive-contrast: #ffffff;
    --uui-color-default: #a01828;
    --uui-color-default-emphasis: #7e1220;
    --uui-color-default-contrast: #ffffff;
    --uui-color-focus: #a01828;
  }
`;
document.head.appendChild(style);
