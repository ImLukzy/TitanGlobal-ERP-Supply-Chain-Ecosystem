import { Navigate, Route, Routes } from 'react-router';
import { AppLayout } from './layout/AppLayout';
import { pantallas } from './modules/pantallas';
import { Inicio } from './pages/Inicio';
import { ModuloPendiente } from './pages/ModuloPendiente';
import { useParams } from 'react-router';

function ModuloPorSlug() {
  const { slug } = useParams();
  const Pantalla = slug === undefined ? undefined : pantallas[slug];

  if (Pantalla !== undefined) {
    return <Pantalla />;
  }
  return <ModuloPendiente />;
}

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Inicio />} />
        <Route path="modulos/:slug" element={<ModuloPorSlug />} />
        <Route path="*" element={<Navigate replace to="/" />} />
      </Route>
    </Routes>
  );
}
