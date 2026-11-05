import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router';
import { AppRoutes } from './App';
import { AccesoProtegido } from './layout/AccesoProtegido';
import { RoleProvider } from './layout/RoleContext';
import { SesionProvider } from './layout/SesionContext';
import './styles/tokens.css';
import './styles/app.css';

const rootElement = document.getElementById('root');

if (rootElement === null) {
  throw new Error('No se encontró el elemento #root.');
}

createRoot(rootElement).render(
  <StrictMode>
    <BrowserRouter>
      <SesionProvider>
        <AccesoProtegido>
          <RoleProvider>
            <AppRoutes />
          </RoleProvider>
        </AccesoProtegido>
      </SesionProvider>
    </BrowserRouter>
  </StrictMode>,
);
