(() => {
  const kiosk = window.location.pathname.startsWith('/kiosk');
  const manifest = document.createElement('link');
  manifest.rel = 'manifest';
  manifest.href = kiosk ? '/kiosk.webmanifest' : '/manifest.webmanifest';
  document.head.appendChild(manifest);

  if (kiosk) {
    const themeColor = document.querySelector('meta[name="theme-color"]');
    if (themeColor) themeColor.setAttribute('content', '#0f172a');
  }
})();
