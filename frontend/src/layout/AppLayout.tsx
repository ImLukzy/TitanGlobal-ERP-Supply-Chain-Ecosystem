import { useEffect, useRef, useState } from 'react';
import { NavLink, Outlet } from 'react-router';
import { isUserRole } from '../api/roleStore';
import { dominios } from '../modules/catalogo';
import { useRole } from './RoleContext';

export function AppLayout() {
  const [menuOpen, setMenuOpen] = useState(false);
  const [expandedDomains, setExpandedDomains] = useState<string[]>([dominios[0].nombre]);
  const menuToggleRef = useRef<HTMLButtonElement>(null);
  const { role, setRole } = useRole();

  useEffect(() => {
    if (!menuOpen) {
      return;
    }
    function handleKeyDown(event: KeyboardEvent): void {
      if (event.key === 'Escape') {
        setMenuOpen(false);
        menuToggleRef.current?.focus();
      }
    }
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [menuOpen]);

  return (
    <div className="app-shell">
      <header className="topbar">
        <button
          aria-expanded={menuOpen}
          aria-label={menuOpen ? 'Cerrar navegación' : 'Abrir navegación'}
          className="menu-toggle"
          ref={menuToggleRef}
          onClick={() => setMenuOpen((open) => !open)}
          type="button"
        >
          <span aria-hidden="true">☰</span>
        </button>
        <NavLink className="brand" to="/" onClick={() => setMenuOpen(false)}>
          <span className="brand-mark" aria-hidden="true">T</span>
          <span><strong>TitanGlobal</strong><small>ERP · Supply Chain</small></span>
        </NavLink>
        <label className="role-picker">
          <span>Rol de prueba</span>
          <select
            aria-label="Rol de prueba"
            onChange={(event) => {
              const value = event.currentTarget.value;
              setRole(isUserRole(value) ? value : null);
            }}
            value={role ?? ''}
          >
            <option value="">Ninguno</option>
            <option value="Administrador">Administrador</option>
            <option value="Bloqueado">Bloqueado</option>
            <option value="SinPermiso">SinPermiso</option>
          </select>
        </label>
      </header>
      <div className="workspace">
        <button
          aria-label="Cerrar navegación"
          className={`sidebar-scrim${menuOpen ? ' is-visible' : ''}`}
          onClick={() => setMenuOpen(false)}
          tabIndex={menuOpen ? 0 : -1}
          type="button"
        />
        <aside aria-label="Navegación principal" className={`sidebar${menuOpen ? ' is-open' : ''}`}>
          <div className="sidebar-heading">Espacios de trabajo</div>
          <NavLink className={({ isActive }) => `nav-home${isActive ? ' is-active' : ''}`} onClick={() => setMenuOpen(false)} to="/">
            <span aria-hidden="true">⌂</span> Inicio
          </NavLink>
          <nav aria-label="Dominios y módulos" className="domain-list">
            {dominios.map((dominio, indice) => (
              <details className="domain-group" key={dominio.nombre} open={expandedDomains.includes(dominio.nombre)}>
                <summary
                  aria-expanded={expandedDomains.includes(dominio.nombre)}
                  aria-label={dominio.nombre}
                  onClick={(event) => {
                    event.preventDefault();
                    setExpandedDomains((current) => current.includes(dominio.nombre)
                      ? current.filter((nombre) => nombre !== dominio.nombre)
                      : [...current, dominio.nombre]);
                  }}
                >
                  <span className="domain-icon" aria-hidden="true">{String(indice + 1).padStart(2, '0')}</span>
                  <span>{dominio.nombre}</span>
                  <span className="domain-count">10</span>
                </summary>
                <ul>
                  {dominio.modulos.map((modulo) => (
                    <li key={modulo.slug}>
                      <NavLink
                        className={({ isActive }) => `module-link${isActive ? ' is-active' : ''}`}
                        onClick={() => setMenuOpen(false)}
                        to={`/modulos/${modulo.slug}`}
                      >
                        <span>{String(modulo.numero).padStart(2, '0')}</span>
                        <span>{modulo.nombre}</span>
                      </NavLink>
                    </li>
                  ))}
                </ul>
              </details>
            ))}
          </nav>
          <div className="sidebar-footer"><span className="status-dot" /> Entorno de desarrollo</div>
        </aside>
        <main className="main-content"><Outlet /></main>
      </div>
    </div>
  );
}
