const style = document.createElement('style');
style.textContent = `
  :root {
    --uui-color-interactive: #d02d38;
    --uui-color-interactive-emphasis: #a82230;
    --uui-color-interactive-standalone: #e85a65;
    --uui-color-focus: #d02d38;
    --uui-color-current: #d02d38;
    --uui-color-selected: rgba(208,45,56,0.12);
    --uui-color-default: #d02d38;
    --uui-color-default-emphasis: #a82230;
    --uui-color-default-standalone: #e85a65;
  }
`;
document.head.appendChild(style);
