import { Navigate, Route, Routes } from 'react-router';
import { AppLayout } from './layout/AppLayout';
import { Inicio } from './pages/Inicio';
import { ModuloPendiente } from './pages/ModuloPendiente';

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Inicio />} />
        <Route path="modulos/:slug" element={<ModuloPendiente />} />
        <Route path="*" element={<Navigate replace to="/" />} />
      </Route>
    </Routes>
  );
}
