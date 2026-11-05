# Crear la pantalla de un módulo en React

Guía paso a paso para implementar la interfaz React de un módulo nuevo en el frontend (`frontend/src/modules/<modulo>/`). Es la continuación de [`docs/equipo/crear-modulo.md`](crear-modulo.md), que cubre el backend. Sigue como ejemplo real `frontend/src/modules/proveedores/` (para pantallas con líneas de detalle, máquinas de estados y varios componentes, toma como referencia `frontend/src/modules/solicitudes-compra/`).

Las pantallas no dependen de las cuatro capas del backend (`Domain`, `Application`, `Infrastructure`, `Api`): solo consumen los endpoints HTTP de la API (`/api/v1/...`).

---

## Antes de empezar

Antes de programar la pantalla, el backend del módulo debe estar implementado en `main` o en tu rama, con la base de datos local y la API levantadas.

1. **Levantar base de datos y aplicar migraciones:**
   ```sh
   docker compose up -d db
   dotnet tool restore
   dotnet ef database update -p src/TitanGlobal.Infrastructure -s src/TitanGlobal.Api
   docker compose exec -T db psql -U titanglobal -d titanglobal -f - < scripts/seed-dev.sql
   ```
   El último comando carga los datos de ejemplo. En PowerShell usa la variante de [`base-de-datos-local.md`](base-de-datos-local.md).
2. **Arrancar la API y el frontend en dos terminales separadas:**
   ```sh
   # Terminal 1: API backend en puerto 5080
   dotnet run --project src/TitanGlobal.Api --urls http://localhost:5080

   # Terminal 2: Servidor de desarrollo Vite en puerto 5173
   npm --prefix frontend ci
   npm --prefix frontend run dev
   ```
3. **Puertos y URLs reales:**
   - **API:** `http://localhost:5080` (salud: `http://localhost:5080/api/v1/sistema/salud` → `{"estado":"OK"}`).
   - **Frontend:** `http://localhost:5173`.
   - **PostgreSQL:** puerto `5433` (usuario `titanglobal`, base `titanglobal`, contraseña del archivo `docker-compose.yml`).
4. **Consultar el contrato:**
   - Abre `http://localhost:5080/swagger` en tu navegador. Swagger UI documenta todas las rutas `api/v1/...` y permite ejecutar peticiones. Toda la API exige token: ejecuta `POST /api/v1/autenticacion/login` con `admin` / `Admin2026!` (seed local), copia el `token` y pégalo en **Authorize** → **Bearer**. En el mismo botón puedes enviar la cabecera `X-User-Role`: la API rechaza con 403 los roles `Bloqueado` y `SinPermiso`, y el resto de valores pasa.
   - El frontend abre con la pantalla de inicio de sesión. En local entra con `admin` / `Admin2026!` o con la cuenta de demostración `invitado` / `Demo2026!` (sin rol `ADMIN`: no puede crear, editar ni borrar usuarios o roles).
   - Revisa `http://localhost:5080/openapi/v1.json` para verificar el contrato OpenAPI exacto: nombres de propiedades en camelCase, tipos de datos, parámetros de query y códigos de estado HTTP.

---

## Qué va en cada archivo

| Archivo | Qué contiene | Qué debe cumplir / Prohibido |
|---|---|---|
| `docs/specs/moduloNN-nombre-frontend.md` | Especificación de la pantalla | Nombre **sin número global**. Define componentes compartidos, criterios de aceptación A1–A6 medibles y mensajes literales de error. |
| `frontend/src/modules/<modulo>/tipos.ts` | Interfaces TypeScript del módulo | Alineadas con el OpenAPI (`/openapi/v1.json`) y los DTOs de `Application`. Prohibido el tipo `any`. |
| `frontend/src/modules/<modulo>/api.ts` | Funciones asíncronas HTTP | Utiliza `apiGet`, `apiPost`, `apiPut`, `apiDelete` de `../../api/client`. Omite valores vacíos (`''`) y `undefined` de la query string. |
| `frontend/src/modules/<modulo>/<Modulo>Page.tsx` | Componente principal de la vista | Envuelto en `<PaginaModulo>`. Usa componentes compartidos (`TablaPaginada`, `PanelDetalle`, `DialogoBaja`, `CampoConError`). |
| `frontend/src/modules/<modulo>/api.test.ts` | Pruebas de integración HTTP con Vitest | Mockea `fetch` con `vi.stubGlobal`. Comprueba URL, método, query sin filtros vacíos y payload JSON exacto de cada función. |
| `frontend/src/modules/<modulo>/<Modulo>Page.test.tsx` | Pruebas de interfaz y flujos de usuario | Usa `esperarCargaInicial...`. Prohibido temporizadores reales en tests con debounce. Cubre filtros, validación local, error 409 y baja accesible. |
| `frontend/src/modules/pantallas.ts` | Registro en el router del ERP | Asocia el slug `'NN-slug'` (generado por `catalogo.ts`) al componente de la página. |
| `frontend/src/styles/app.css` | Reglas de estilo propias del módulo | Bloque nuevo al final del archivo con prefijo del módulo (ej. `.<modulo>-...`). Prohibido alterar selectores compartidos globales o elevar su especificidad. |

---

## Paso a paso

### 1. Spec

1. Copia [`docs/specs/_TEMPLATE.md`](../specs/_TEMPLATE.md) a `docs/specs/moduloNN-nombre-frontend.md`, **sin número global** (varias personas elegían el mismo número a la vez; solo las specs transversales llevan número global).
2. Define en la spec:
   - **§1 Objetivo:** problema que resuelve la pantalla y resultado esperado.
   - **§2 Fuera de alcance:** backend (ya probado), librerías externas adicionales o cambios en pantallas existentes.
   - **§3 Archivos afectados:** tabla con los archivos del módulo en `frontend/src/modules/<modulo>/`, registro en `pantallas.ts` y estilos en `app.css`.
   - **§4 Diseño y lógica:** resumen de métricas, filtros y debounce (300 ms), tabla con paginación, panel de detalle lateral o apilado, formulario con validaciones locales y diálogo de baja.
   - **§5 Criterios de aceptación (A1–A6):** A1 Gate en verde, A2 API y querys exactas, A3 Filtros y tabla, A4 Detalle, formulario y baja accesible con devolución de foco, A5 Sin regresiones en otros módulos, A6 QA visual a 375 px y 1280 px en claro y oscuro.
3. Pide revisión a Lukas antes de programar y crea tu rama desde `main` actualizado:
   ```sh
   git fetch origin
   git switch -c tu-nombre/moduloNN-nombre-frontend origin/main
   ```

### 2. Tipos alineados con OpenAPI (`tipos.ts`)

Crea `frontend/src/modules/<modulo>/tipos.ts`. Los nombres de las propiedades deben coincidir exactamente con los esquemas de `/openapi/v1.json` y los DTOs de `TitanGlobal.Application` (en camelCase).

Ejemplo real (`frontend/src/modules/proveedores/tipos.ts`):

```typescript
export interface ProveedorListado {
  id: number;
  razonSocial: string;
  ruc: string;
  pais: string;
  activo: boolean;
}

export interface PaginaProveedores {
  items: ProveedorListado[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export interface ProveedorDetalle extends ProveedorListado {
  email: string | null;
  telefono: string | null;
}

export interface GuardarProveedorRequest {
  razonSocial: string;
  ruc: string;
  pais: string;
  email?: string;
  telefono?: string;
  activo: boolean;
}

export interface PaisProveedores {
  pais: string;
  totalProveedores: number;
  activos: number;
}

export interface ResumenProveedores {
  totalProveedores: number;
  activos: number;
  inactivos: number;
  sinEmail: number;
  porPais: PaisProveedores[];
}

export interface FiltrosProveedor {
  busqueda?: string;
  pais?: string;
  activo?: boolean | '';
  pagina?: number;
  tamanoPagina?: number;
}

export interface IdResponse {
  id: number;
}
```

### 3. Funciones de API y tratamiento de errores (`api.ts`)

Implementa en `frontend/src/modules/<modulo>/api.ts` las funciones de consulta y mutación utilizando el cliente HTTP centralizado en `frontend/src/api/client.ts`.

#### Cómo trata los errores el cliente HTTP

El cliente compartido `solicitar<T>` en `frontend/src/api/client.ts`:
- Envía automáticamente `Authorization: Bearer <token>` de la sesión iniciada, la cabecera `X-Usuario` con el usuario de la sesión y `X-User-Role` con el rol simulado (`getSelectedRole()`). Si la API responde 401, borra la sesión y vuelve la pantalla de inicio de sesión.
- Si la respuesta HTTP no es exitosa (`!response.ok`), inspecciona el cuerpo JSON. El backend de TitanGlobal devuelve los errores de negocio formateados como `{ StatusCode, Message }` a través del middleware de excepciones. `client.ts` extrae esa propiedad `Message` y lanza una instancia de `ApiError`:
  ```typescript
  export class ApiError extends Error {
    readonly statusCode: number;
    constructor(statusCode: number, message: string) {
      super(message);
      this.name = 'ApiError';
      this.statusCode = statusCode;
    }
  }
  ```
- Si el fallo es de red o DNS (captura `TypeError` en el `fetch`), lanza un `ApiError(0, 'No se pudo conectar con la API.')`.
- En caso de código 500 no estructurado, lanza un `ApiError(500, 'La API no está disponible o no respondió correctamente.')`.

#### Cómo construir la query string limpia

Para no enviar parámetros residuales a la API (`?busqueda=&pais=`), itera sobre los filtros y añade únicamente aquellos cuyo valor sea distinto de cadena vacía `''` y de `undefined`.

Ejemplo real (`frontend/src/modules/proveedores/api.ts`):

```typescript
import { apiDelete, apiGet, apiPost, apiPut } from '../../api/client';
import type {
  FiltrosProveedor,
  GuardarProveedorRequest,
  IdResponse,
  PaginaProveedores,
  ProveedorDetalle,
  ResumenProveedores,
} from './tipos';

export function listarProveedores(filtros: FiltrosProveedor): Promise<PaginaProveedores> {
  const consulta = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== '' && valor !== undefined) {
      consulta.set(clave, String(valor));
    }
  }
  const query = consulta.toString();
  const separador = query === '' ? '' : `?${query}`;
  return apiGet<PaginaProveedores>(`/proveedores${separador}`);
}

export function obtenerProveedor(id: number): Promise<ProveedorDetalle> {
  return apiGet<ProveedorDetalle>(`/proveedores/${id}`);
}

export function crearProveedor(request: GuardarProveedorRequest): Promise<{ id: number }> {
  return apiPost<{ id: number }>('/proveedores', request);
}

export function actualizarProveedor(id: number, request: GuardarProveedorRequest): Promise<IdResponse> {
  return apiPut<IdResponse>(`/proveedores/${id}`, request);
}

export function eliminarProveedor(id: number): Promise<void> {
  return apiDelete(`/proveedores/${id}`);
}

export function listarPaisesProveedor(): Promise<string[]> {
  return apiGet<string[]>('/proveedores/paises');
}

export function obtenerResumenProveedores(): Promise<ResumenProveedores> {
  return apiGet<ResumenProveedores>('/proveedores/resumen');
}
```

### 4. La página con los componentes compartidos

Crea `frontend/src/modules/<modulo>/<Modulo>Page.tsx`. Toda pantalla debe integrarse con los componentes reutilizables de `frontend/src/components/`:

- **`PaginaModulo` (`PaginaModulo.tsx`):** resuelve el módulo en `catalogo.ts` por número o slug (ej. `modulo="11-proveedores"`), genera la miga de pan accesible (`<nav aria-label="Miga de pan" className="breadcrumbs">`), el eyebrow `MÓDULO NN · <DOMINIO>` y el encabezado `<h1>`.
- **`TablaPaginada` (`TablaPaginada.tsx`):** contenedor con scroll horizontal accesible (`role="region"`, `tabIndex={0}`, `aria-label`), tabla con `<caption>`, cabeceras `<th>` con `scope="col"`, renderizado de filas y barra de paginación accesible (`Página X de Y`) con botones Anterior / Siguiente.
- **`PanelDetalle` (`PanelDetalle.tsx`):** sección semántica (`<section aria-labelledby="...">`) para mostrar la ficha del elemento seleccionado de forma lateral en escritorio o apilada en móvil.
- **`CampoConError` (`CampoConError.tsx`):** contenedor `.form-field` que asocia `<label htmlFor="...">`, input y `<span className="field-error" id="...">`.
- **`DialogoBaja` (`DialogoBaja.tsx`):** ventana modal accesible para confirmar la eliminación de un registro.

Fragmento real de la estructura principal (`frontend/src/modules/proveedores/ProveedoresPage.tsx`, recortado para mostrar el layout general):

```tsx
export function ProveedoresPage() {
  // Estados para búsqueda, filtros, paginación, selección y modales
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtros, setFiltros] = useState<{ pais: string; activo: boolean | ''; pagina: number }>({
    pais: '',
    activo: '',
    pagina: 1,
  });
  const [lista, setLista] = useState<PaginaProveedores | null>(null);
  const [resumen, setResumen] = useState<ResumenProveedores | null>(null);
  const [paises, setPaises] = useState<string[]>([]);
  const [seleccionado, setSeleccionado] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<ProveedorDetalle | null>(null);
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [dialogoAbierto, setDialogoAbierto] = useState(false);

  // Debounce de 300 ms en la búsqueda
  useEffect(() => {
    const temporizador = window.setTimeout(() => setBusquedaAplicada(busqueda), 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  // Al cambiar filtros o búsqueda, se reinicia la paginación a la página 1
  function cambiarFiltro(cambios: Partial<typeof filtros>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
  }

  const paginas = Math.max(1, Math.ceil((lista?.total ?? 0) / TAMANO_PAGINA));

  return (
    <PaginaModulo className="proveedores-page" modulo="11-proveedores">
      <p className="module-intro">Administra los datos y el estado de tus proveedores.</p>
      <Resumen datos={resumen} error={errorResumen} />
      <div className="proveedores-work-area">
        <section aria-labelledby="proveedores-list-title" className="clientes-list-panel proveedores-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">DIRECTORIO</div>
              <h2 id="proveedores-list-title">Todos los proveedores</h2>
            </div>
            <button className="primary-button" onClick={() => setMostrarFormulario(true)} type="button">
              Nuevo proveedor
            </button>
          </div>
          <div className="proveedores-filters">
            {/* Controles de búsqueda con debounce y selectores de país y estado */}
          </div>
          {lista !== null && lista.items.length > 0 && (
            <TablaPaginada
              caption="Directorio de proveedores"
              etiquetaRegion="Tabla de proveedores"
              columnas={[
                { id: 'razonSocial', etiqueta: 'Razón social' },
                { id: 'ruc', etiqueta: 'RUC' },
                { id: 'pais', etiqueta: 'País' },
                { id: 'activo', etiqueta: 'Estado' },
              ]}
              elementos={lista.items}
              obtenerClave={(proveedor) => proveedor.id}
              pagina={filtros.pagina}
              totalPaginas={paginas}
              cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
              className="proveedores-table"
              classNameContenedor="proveedores-table-scroll"
              classNamePaginacion="clientes-pagination"
              renderizarFila={(proveedor) => (
                <tr className={seleccionado === proveedor.id ? 'is-selected' : ''}>
                  <td>
                    <button
                      aria-current={seleccionado === proveedor.id ? 'true' : undefined}
                      className="cliente-row-select"
                      onClick={() => setSeleccionado(proveedor.id)}
                      type="button"
                    >
                      {proveedor.razonSocial}
                    </button>
                  </td>
                  <td>{proveedor.ruc}</td>
                  <td>{proveedor.pais}</td>
                  <td>
                    <span className={`proveedor-status${proveedor.activo ? ' is-active' : ' is-inactive'}`}>
                      {proveedor.activo ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                </tr>
              )}
            />
          )}
        </section>
        <PanelDetalle ariaLabelledBy="proveedor-detail-title" className="clientes-detail-panel proveedores-detail-panel">
          {/* Ficha de detalle o mensaje de selección */}
        </PanelDetalle>
      </div>
    </PaginaModulo>
  );
}
```

### 5. Formularios y validación

Los formularios del frontend deben validar los datos localmente antes de enviar la petición y gestionar adecuadamente los errores de la API.

1. **Validación local inmediata:**
   Valida campos obligatorios, longitudes máximas y expresiones regulares con los mismos mensajes de error literales definidos en la spec del backend. Si hay errores locales, asigna el diccionario de errores al estado y no llames a la API (`return;`).
2. **Accesibilidad con `CampoConError`:**
   Enlaza cada input con su mensaje de error mediante `aria-describedby` y marca `aria-invalid={true}` cuando exista error.
3. **Gestión de errores 400 y 409 del backend:**
   Si la API devuelve un conflicto (por ejemplo, RUC duplicado con código 409), captura el error mediante `ApiError`, muestra `error.message` en un elemento con `role="alert"` y **conserva intactos todos los valores que el usuario escribió**. Nunca limpies los campos ante un fallo de la API.

Fragmento real de validación y formulario (`frontend/src/modules/proveedores/ProveedoresPage.tsx`, recortado para ilustrar los campos principales):

```tsx
function validarProveedor(valores: GuardarProveedorRequest): ErroresProveedor {
  const errores: ErroresProveedor = {};
  const razonSocial = valores.razonSocial.trim();
  const ruc = valores.ruc.trim();
  const pais = valores.pais.trim();
  const email = valores.email?.trim() ?? '';
  const telefono = valores.telefono?.trim() ?? '';

  if (razonSocial === '') {
    errores.razonSocial = 'La razón social es obligatoria.';
  } else if (razonSocial.length > 200) {
    errores.razonSocial = 'La razón social no puede superar 200 caracteres.';
  }
  if (!/^\d{11}$/.test(ruc)) {
    errores.ruc = 'El RUC debe tener 11 dígitos numéricos.';
  }
  if (pais === '') {
    errores.pais = 'El país es obligatorio.';
  } else if (pais.length > 100) {
    errores.pais = 'El país no puede superar 100 caracteres.';
  }
  if (email !== '' && (email.length > 150 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))) {
    errores.email = 'El email no tiene un formato válido.';
  }
  if (telefono !== '' && !/^[\d +-]{6,20}$/.test(telefono)) {
    errores.telefono = 'El teléfono no tiene un formato válido.';
  }
  return errores;
}

function FormularioProveedor({ proveedor, paises, cancelar, guardado }: {
  proveedor: ProveedorDetalle | null;
  paises: string[];
  cancelar: () => void;
  guardado: () => Promise<void>;
}) {
  const [razonSocial, setRazonSocial] = useState(proveedor?.razonSocial ?? '');
  const [ruc, setRuc] = useState(proveedor?.ruc ?? '');
  const [pais, setPais] = useState(proveedor?.pais ?? '');
  const [email, setEmail] = useState(proveedor?.email ?? '');
  const [telefono, setTelefono] = useState(proveedor?.telefono ?? '');
  const [activo, setActivo] = useState(proveedor?.activo ?? true);
  const [errores, setErrores] = useState<ErroresProveedor>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const request: GuardarProveedorRequest = {
      razonSocial: razonSocial.trim(),
      ruc: ruc.trim(),
      pais: pais.trim(),
      activo,
    };
    if (email.trim() !== '') {
      request.email = email.trim();
    }
    if (telefono.trim() !== '') {
      request.telefono = telefono.trim();
    }
    const nuevosErrores = validarProveedor(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setGuardando(true);
    try {
      if (proveedor === null) {
        await crearProveedor(request);
      } else {
        await actualizarProveedor(proveedor.id, request);
      }
      await guardado();
    } catch (error: unknown) {
      // Muestra el mensaje literal del backend (ej. "Ya existe un proveedor con el RUC...")
      // y mantiene los campos intactos
      setMensaje(mensajeError(error, 'No se pudo guardar el proveedor.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="proveedor-form-title" className="clientes-form-panel proveedores-form-panel">
      <div className="clientes-panel-heading">
        <h2 id="proveedor-form-title">{proveedor === null ? 'Nuevo proveedor' : 'Editar proveedor'}</h2>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="proveedor-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Razón social" htmlFor="proveedor-razon-social" error={errores.razonSocial} idError="proveedor-razon-social-error">
          <input
            aria-describedby={errores.razonSocial ? 'proveedor-razon-social-error' : undefined}
            aria-invalid={Boolean(errores.razonSocial)}
            id="proveedor-razon-social"
            maxLength={220}
            onChange={(event) => setRazonSocial(event.currentTarget.value)}
            value={razonSocial}
          />
        </CampoConError>
        {/* Resto de campos usando CampoConError */}
        <div className="proveedores-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar proveedor'}
          </button>
        </div>
      </form>
    </section>
  );
}
```

### 6. Baja con diálogo accesible

Para dar de baja o eliminar un registro, usa el componente reutilizable `DialogoBaja` (`frontend/src/components/DialogoBaja.tsx`).

`DialogoBaja` siempre escribe «Eliminar {entidad}»; su texto no se puede cambiar por ahora. Tenlo en cuenta si tu módulo hace baja lógica.

Propiedades de accesibilidad garantizadas por `DialogoBaja`:
- Contenedor modal con `role="dialog"` y `aria-modal="true"`.
- Trampa de foco (`Tab` y `Shift+Tab`) entre los botones interactivos del diálogo.
- Cancelación con la tecla `Escape` sin emitir peticiones.
- Al cerrarse el diálogo, debe devolverse el foco al botón que lo abrió utilizando `window.requestAnimationFrame`.

Fragmento real de integración (`frontend/src/modules/proveedores/ProveedoresPage.tsx`):

```tsx
const botonEliminarRef = useRef<HTMLButtonElement>(null);

const cerrarDialogo = useCallback(() => {
  setDialogoAbierto(false);
  setErrorEliminar('');
  window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
}, []);

async function confirmarEliminacion(): Promise<void> {
  if (detalle === null) {
    return;
  }
  setEliminando(true);
  setErrorEliminar('');
  try {
    await eliminarProveedor(detalle.id);
    cerrarDialogo();
    setSeleccionado(null);
    setDetalle(null);
    setActualizacion((valor) => valor + 1);
  } catch (error: unknown) {
    setErrorEliminar(mensajeError(error, 'No se pudo eliminar el proveedor.'));
  } finally {
    setEliminando(false);
  }
}

// En el JSX:
{dialogoAbierto && detalle !== null && (
  <DialogoBaja
    entidad="proveedor"
    nombre={detalle.razonSocial}
    cerrar={cerrarDialogo}
    confirmar={() => void confirmarEliminacion()}
    eliminando={eliminando}
    error={errorEliminar}
  />
)}
```

### 7. Estilos CSS y soporte para modo oscuro

#### Cuándo basta con lo existente
`frontend/src/styles/app.css` ya proporciona clases compartidas para la estructura común:
- **Botones:** `.primary-button`, `.secondary-button`, `.danger-button`.
- **Paneles:** `.clientes-list-panel`, `.clientes-detail-panel`, `.clientes-form-panel`, `.clientes-panel-heading`.
- **Métricas:** `.cliente-stat`.
- **Mensajes:** `.clientes-error`, `.clientes-status`, `.field-error`.
- **Tablas:** `.tabla-paginada-scroll`, `.tabla-paginada`, `.tabla-paginada-nav`.
- **Acciones y selección:** `.cliente-row-select`, `.is-selected`.

#### Cómo añadir tu bloque en `frontend/src/styles/app.css`
1. Añade tu bloque al **final de `app.css`** precedido por un comentario con el nombre del módulo.
2. Prefija todas tus clases con el nombre del módulo (ej. `.proveedores-summary`, `.proveedores-filters`, `.proveedores-table`).
3. **Prohibido modificar selectores compartidos:** nunca edites ni aumentes la especificidad de selectores globales como `th`, `.tabla-paginada th` o `.cliente-stat`. Subir la especificidad (por ejemplo, cambiar `th` a `thead th`) altera las cabeceras de tabla de pantallas previamente integradas.
4. **Uso de tokens para modo claro y oscuro:** utiliza siempre las variables CSS de `frontend/src/styles/tokens.css` (`var(--surface)`, `var(--page)`, `var(--ink)`, `var(--ink-soft)`, `var(--ink-faint)`, `var(--line)`, `var(--accent)`, `var(--accent-soft)`, `var(--accent-ink)`, `var(--good)`, `var(--good-soft)`, `var(--bad)`, `var(--bad-soft)`). Prohibido usar colores fijos como `#fff` o `#000` en textos o fondos.
5. **Cifras e importes en una sola línea:** aplica siempre `white-space: nowrap` y `font-variant-numeric: tabular-nums` en montos, fechas y etiquetas de estado.
6. **Diseño responsive:** define maquetación en dos columnas a partir de 1024 px y apilada en pantallas menores de 720 px.

Fragmento real de estilos (`frontend/src/styles/app.css`, reglas de Proveedores):

```css
.proveedores-page { padding-bottom: 42px; }
.proveedores-summary { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }
.proveedores-country-panel { grid-column: 1 / -1; min-width: 0; padding: 16px 18px; border: 1px solid var(--line); border-radius: 14px; background: var(--surface); box-shadow: var(--shadow); }
.proveedores-country-panel h2 { margin: 0 0 10px; color: var(--ink-soft); font-size: 12px; }
.proveedores-work-area { display: grid; grid-template-columns: minmax(0, 1fr); align-items: start; gap: 0 15px; }
.proveedores-list-panel, .proveedores-detail-panel { min-width: 0; }
.proveedores-filters { display: grid; grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr) minmax(0, 1fr); align-items: center; gap: 7px 10px; margin-bottom: 14px; }
.proveedores-table { width: 100%; min-width: 500px; border-collapse: collapse; text-align: left; font-size: 10px; }
.proveedores-table-scroll { width: 100%; max-width: 100%; overflow-x: auto; overscroll-behavior-x: contain; }
.proveedores-table th { padding: 9px 8px; background: var(--surface-muted); color: var(--ink-soft); font-size: 9px; font-weight: 700; }
.proveedores-table td { padding: 9px 8px; border-bottom: 1px solid var(--line); color: var(--ink); overflow-wrap: anywhere; }
.proveedores-table tbody tr.is-selected { background: var(--accent-soft); }
.proveedor-status { display: inline-flex; padding: 4px 7px; border-radius: 20px; font-size: 9px; font-weight: 700; white-space: nowrap; }
.proveedor-status.is-active { background: var(--good-soft); color: var(--good); }
.proveedor-status.is-inactive { background: var(--bad-soft); color: var(--bad); }

@media (min-width: 1024px) {
  .proveedores-work-area { grid-template-columns: minmax(0, 1.2fr) minmax(300px, .8fr); }
  .proveedores-table { min-width: 0; table-layout: fixed; }
  .proveedores-table th, .proveedores-table td { padding: 8px 6px; overflow-wrap: anywhere; }
}

@media (max-width: 720px) {
  .proveedores-summary { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px; }
  .proveedores-summary .proveedores-country-panel { grid-column: 1 / -1; }
  .proveedores-work-area { grid-template-columns: minmax(0, 1fr); gap: 0; }
  .proveedores-filters { grid-template-columns: minmax(0, 1fr); gap: 6px; }
  .proveedores-table { min-width: 520px; }
  .proveedor-form { grid-template-columns: minmax(0, 1fr); }
}
```

### 8. Registro en el router y catálogo

1. **`frontend/src/modules/pantallas.ts`:** importa tu página y regístrala al final del objeto `pantallas`:
   ```typescript
   import { ProveedoresPage } from './proveedores/ProveedoresPage';

   export const pantallas: Record<string, ComponentType> = {
     // ...
     '11-proveedores': ProveedoresPage,
   };
   ```
2. **`frontend/src/modules/catalogo.ts`:** comprueba que el módulo figura en la lista `nombresPorDominio` de su dominio correspondiente. La función `modulos` calcula automáticamente el slug en kebab-case (`NN-nombre`). La clave registrada en `pantallas.ts` debe coincidir exactamente con el slug generado.

### 9. Pruebas con Vitest

Se requieren dos tipos de pruebas: unitarias de la API (`api.test.ts`) y de integración de la pantalla (`<Modulo>Page.test.tsx`).

#### Pruebas de API (`api.test.ts`)
Mockea `fetch` con `vi.stubGlobal('fetch', fetchMock)`, comprueba URLs, métodos HTTP, serialización del cuerpo y que los parámetros vacíos o `undefined` no aparezcan en la query string.

Fragmento real (`frontend/src/modules/proveedores/api.test.ts`):

```typescript
import { afterEach, describe, expect, it, vi } from 'vitest';
import { listarProveedores, crearProveedor } from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

function respuestaJson(contenido: unknown, status = 200): Response {
  return Response.json(contenido, { status });
}

describe('API de proveedores', () => {
  it('omite filtros vacíos y undefined', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ items: [], total: 0 }));
    vi.stubGlobal('fetch', fetchMock);
    await listarProveedores({ busqueda: '', pais: undefined, activo: undefined, pagina: 1, tamanoPagina: 20 });
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores?pagina=1&tamanoPagina=20');
  });

  it('crea proveedores con POST y el cuerpo exacto', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respuestaJson({ id: 7 }));
    vi.stubGlobal('fetch', fetchMock);
    const request = { razonSocial: 'Nuevo Proveedor', ruc: '20777777777', pais: 'Perú', activo: true };
    await crearProveedor(request);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/v1/proveedores');
    expect(fetchMock.mock.calls[0]?.[1]).toMatchObject({ method: 'POST', body: JSON.stringify(request) });
  });
});
```

#### Pruebas de la pantalla (`<Modulo>Page.test.tsx`)
Reglas obligatorias según `docs/equipo/lecciones.md`:
- **Prohibido temporizadores reales:** no uses `setTimeout` ni `sleep`. Para los 300 ms del debounce de búsqueda, usa `vi.useFakeTimers()` y `vi.advanceTimersByTimeAsync(300)`.
- **Helper `esperarCargaInicial...`:** cada pantalla debe tener una función que espere a que terminen todas las peticiones concurrentes iniciales (listado, resumen, catálogos) y compruebe que el listado se solicitó una sola vez. No hagas `mockClear` antes de esta espera.
- **Validación local sin petición:** verifica que enviar el formulario con datos inválidos muestre los errores en pantalla sin llamar a `POST`.
- **Conservación de datos ante error 409:** verifica que si el servidor rechaza la creación por unicidad, el mensaje de error se muestre con `role="alert"` sin borrar lo escrito en los inputs.
- **Baja y tecla Escape:** verifica que al pulsar Escape, el diálogo se cierre sin emitir `DELETE` y el foco regrese al botón de eliminar.

Fragmento real del helper y test con debounce (`frontend/src/modules/proveedores/ProveedoresPage.test.tsx`):

```typescript
async function esperarCargaInicialProveedores(fetchMock: ReturnType<typeof vi.fn>) {
  vi.useFakeTimers();
  render(<ProveedoresPage />);
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  await waitFor(() => {
    expect(screen.getByRole('button', { name: 'Andina Suministros' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Andina Suministros' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'China' })).toBeInTheDocument();
    expect(screen.getByText('Total de proveedores')).toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/proveedores?'))).toHaveLength(1);
  });
}

it('aplica la búsqueda tras 300 ms y solo consulta el último texto', async () => {
  const { fetchMock } = prepararApi();
  await esperarCargaInicialProveedores(fetchMock);

  vi.useFakeTimers();
  fetchMock.mockClear();
  const busqueda = screen.getByRole('searchbox', { name: 'Buscar por razón social o RUC' });
  fireEvent.change(busqueda, { target: { value: 'A' } });
  fireEvent.change(busqueda, { target: { value: 'An' } });
  fireEvent.change(busqueda, { target: { value: 'Andina' } });

  await act(async () => {
    await vi.advanceTimersByTimeAsync(299);
  });
  expect(fetchMock).not.toHaveBeenCalled();

  await act(async () => {
    await vi.advanceTimersByTimeAsync(1);
  });
  expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/proveedores?'))).toHaveLength(1);
  expect(String(fetchMock.mock.calls.find((call) => String(call[0]).includes('/proveedores?'))?.[0])).toContain('busqueda=Andina');
});
```

Antes de entregar, repite la suite 10 veces para garantizar su estabilidad sin carreras asíncronas.

### 10. Control visual (QA a 375 px y 1280 px)

Comprueba la pantalla en el navegador tanto en **modo claro** como en **modo oscuro** (`prefers-color-scheme: dark` en las herramientas de desarrollo):
- **A 1280 px (escritorio):**
  - Dos columnas: lista/tabla a la izquierda y panel de detalle a la derecha.
  - Ninguna cabecera de tabla partida; cifras, importes y fechas en una sola línea.
  - Tarjetas de resumen: número grande y texto descriptivo pequeño abajo.
  - Sin desbordamiento ni scroll horizontal en la ventana principal.
- **A 375 px (móvil):**
  - Vista apilada: detalle debajo del listado.
  - Márgenes laterales de la página de al menos 16 px.
  - Si la tabla es ancha, se desplaza dentro de su propio contenedor `.tabla-paginada-scroll`, nunca haciendo scroll horizontal en la página entera.
- **Modo oscuro:**
  - Contraste de texto y enlaces ≥ 4.5:1 (nunca texto azul oscuro sobre fondo oscuro).
  - Pastillas de estado sin contornos blancos.
  - Fila seleccionada con fondo homogéneo `var(--accent-soft)` en todas sus celdas.

Toma capturas a 375 px y 1280 px con el detalle abierto y con el diálogo de baja para adjuntar a la pull request.

### 11. Gate de verificación

Ejecuta el gate de frontend y el gate completo del repositorio antes de subir tu rama (en Windows, desde Git Bash):

```sh
# Verificación del frontend (lint sin warnings, typecheck, tests y build de Vite)
npm --prefix frontend run verify

# Gate completo del repositorio
./scripts/verify.sh
```

Ambos comandos deben terminar con código de salida 0 y sin warnings.

### 12. Pull request

1. Confirma los cambios con tu identidad de git (`git config`).
2. Sube tu rama a GitHub:
   ```sh
   git push -u origin tu-nombre/moduloNN-nombre-frontend
   ```
3. Abre la pull request completando `.github/pull_request_template.md`. Incluye la salida de `./scripts/verify.sh` y adjunta las 4 capturas visuales (375 px y 1280 px en claro y oscuro). Registra los resultados en el §7 de la spec.

---

## Cuándo partir en varios componentes

En pantallas con flujos sencillos (como `ProveedoresPage.tsx`, con ~600 líneas), basta con mantener los subcomponentes (`Resumen`, `FormularioProveedor`) dentro del mismo archivo si solo los usa esa vista.

Sin embargo, cuando una pantalla incluye **líneas de detalle**, **máquinas de estados** o **múltiples catálogos concurrentes**, debe dividirse en archivos separados para mantener el código desacoplado y comprobable. La referencia del proyecto para este escenario es `frontend/src/modules/solicitudes-compra/`:

| Archivo | Responsabilidad |
|---|---|
| `tipos.ts` | Contratos de la entidad, líneas, catálogos auxiliares y estados (`'Pendiente'`, `'Aprobada'`, `'Rechazada'`). |
| `api.ts` y `api.test.ts` | Funciones HTTP de listado, detalle, creación/edición, transiciones de estado (`aprobarSolicitud`, `rechazarSolicitud`) y catálogos. |
| `useSolicitudesData.ts` | Hook personalizado que encapsula la sincronización con la API: listado, detalle, resumen y carga paralela de catálogos (`Promise.all([listarProveedoresActivos(), listarProductosSolicitud()])`). |
| `SolicitudesResumen.tsx` | Tarjetas métricas superiores con totales por estado y montos acumulados. |
| `SolicitudesFiltros.tsx` | Barra de filtros: búsqueda con debounce, fechas (desde/hasta) con validación y selector de estado. |
| `SolicitudesTabla.tsx` | Tabla principal con `TablaPaginada`, chips de estado y botón de selección por fila. |
| `SolicitudDetallePanel.tsx` | Ficha lateral con datos de cabecera, subtabla de líneas de producto (cantidades y precios) y botones de transición según el estado actual. |
| `SolicitudFormulario.tsx` | Formulario complejo con gestión dinámica de líneas (agregar, modificar y quitar items con recálculo automático del total estimado). |
| `DecisionSolicitudDialog.tsx` | Diálogo accesible para capturar motivos de rechazo o confirmación de aprobación. |
| `SolicitudesOverlays.tsx` | Contenedor de modales secundarios (diálogo de baja, modal de decisión y generación de documentos dependientes). |
| `solicitudes.testUtils.tsx` | Mock unificado de la API, datos base y función compartida `esperarCargaInicial`. |
| Tests por preocupación | `SolicitudesCompraPage.test.tsx` (flujo principal y tabla), `SolicitudesFormulario.test.tsx` (líneas y validaciones), `SolicitudesDecisiones.test.tsx` (transiciones de estado), `SolicitudesGenerarOrden.test.tsx` (integración entre módulos). |

### Criterios para dividir una pantalla:
1. **Líneas o subtablas:** si la entidad contiene colecciones anidadas (artículos, líneas de pedido, cuotas), extrae el formulario y la subtabla a componentes independientes.
2. **Acciones condicionales por estado:** si existen botones de acción que solo aplican a ciertos estados (`Aprobar`, `Rechazar`, `Emitir`), encapsula las acciones y sus diálogos en componentes específicos.
3. **Carga concurrente:** si la pantalla necesita varios catálogos independientes antes de operar, aísla la carga en un custom hook (`use<Modulo>Data.ts`).
4. **Tamaño de pruebas:** si la suite de tests supera los 300 renglones, divídela en archivos temáticos (`<Modulo>Formulario.test.tsx`, `<Modulo>Decisiones.test.tsx`) compartiendo las utilidades en `<modulo>.testUtils.tsx`.

---

## Errores típicos y solución

| Error / Síntoma | Causa | Solución |
|---|---|---|
| Test intermitente que falla con la máquina cargada en el gate | El test usaba `setTimeout` real para probar el debounce de 300 ms | Usar temporizadores falsos de Vitest: `vi.useFakeTimers()` y avanzar con `await act(async () => { await vi.advanceTimersByTimeAsync(300); });` |
| Test falla con `mockClear` o asserciones `findBy` inesperadas | Se realizaron comprobaciones antes de que concluyera la carga inicial | Crear y llamar al inicio del test a la función `esperarCargaInicial...`, esperando listado, resumen y catálogos |
| Rotura de estilos en cabeceras de tabla de otros módulos | Se modificó un selector compartido en `app.css` (ej. cambiar `th` por `thead th` o regla sobre `.tabla-paginada`) | Revertir el selector compartido. Aplicar estilos específicos usando siempre una clase con prefijo del módulo (ej. `.proveedores-table th`) |
| El formulario se vacía tras recibir un 400 o 409 de la API | Se reinició el estado en el bloque `catch` al capturar el error | Mantener las variables de estado intactas en el `catch`; actualizar únicamente el mensaje de error con `error.message` y `role="alert"` |
| Texto azul oscuro sobre fondo oscuro en modo oscuro | Se usaron colores fijos hexadecimales o clases que no respetan el tema | Utilizar siempre tokens semánticos de `tokens.css`: `var(--ink)`, `var(--accent-ink)`, `var(--surface-raised)` |
| Cifras, importes o fechas partidas en dos líneas a 1280 px | Contenedor con ancho insuficiente o falta de regla de no ruptura | Aplicar `white-space: nowrap` y `font-variant-numeric: tabular-nums` a montos, fechas y badges |
| «Pantalla pendiente» al acceder a `/modulos/NN-slug` | Falta registrar el componente de la página en el mapa global | Añadir la entrada `'NN-slug': TuPagina` en `frontend/src/modules/pantallas.ts` |
| `npm run typecheck` falla con `Property 'Message' does not exist on type 'unknown'` | Se accedió directamente a propiedades en un bloque `catch (error)` | Verificar `error instanceof ApiError` para leer `error.message` y `error.statusCode`, o usar una función auxiliar `mensajeError` |
| `npm run lint` reporta error por dependencias de React Hooks | Se omitieron dependencias en el array de `useEffect` o funciones no memoizadas | Envolver funciones estables pasadas por prop con `useCallback` o declarar funciones de efecto dentro del mismo `useEffect` |
| El foco no regresa al botón tras cerrar el diálogo de baja con Escape | No se gestionó la referencia del disparador tras desmontar el diálogo | Guardar la referencia con `useRef<HTMLButtonElement>(null)` y restaurar el foco mediante `window.requestAnimationFrame(() => botonRef.current?.focus())` |
