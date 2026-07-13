import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './theme/workbase.css';
import { initTheme } from './theme/themeMode';
import './i18n';
import App from './App';

initTheme();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
