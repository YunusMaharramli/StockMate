import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './AdminApp';
import './admin.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
