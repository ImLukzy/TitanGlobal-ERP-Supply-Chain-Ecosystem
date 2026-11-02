# Módulo 22 – Categorías de Producto (Frontend)

## 1. Información general

- **Módulo:** 22 – Categorías de Producto.
- **Nombre técnico:** `categorias-producto`.
- **Responsable:** Maxi.
- **Revisor:** Lukas.
- **Tecnologías:** React 19, React Router, Vite y TypeScript.
- **Backend:** API .NET 10.
- **Ruta API:** `/api/v1/categorias-producto`.

## 2. Objetivo

Implementar una interfaz para administrar el catálogo de categorías de productos de TitanGlobal.

La pantalla permitirá consultar, registrar, actualizar y desactivar categorías mediante la API del backend.

Se respetará el diseño y los componentes visuales utilizados en el resto del sistema.

## 3. Alcance funcional

La interfaz incluirá:

1. Visualización del listado de categorías.
2. Búsqueda por nombre.
3. Visualización del estado de cada categoría.
4. Registro de nuevas categorías.
5. Edición de categorías existentes.
6. Desactivación de categorías.
7. Visualización de un resumen general.

## 4. Pantalla de categorías

Se implementará una pantalla principal con:

- Título del módulo.
- Resumen de categorías registradas.
- Indicadores de categorías activas e inactivas.
- Campo de búsqueda.
- Listado de categorías.
- Botón para registrar una categoría.
- Acciones para editar y desactivar registros.

## 5. Formulario de categoría

El formulario permitirá ingresar:

| Campo | Tipo | Validación |
|---|---|---|
| Nombre | Texto | Obligatorio, máximo 100 caracteres. |
| Descripción | Texto | Opcional, máximo 250 caracteres. |

Se utilizará el mismo formulario para crear y editar categorías.

## 6. Integración con el backend

La interfaz consumirá los siguientes endpoints:

| Método | Endpoint | Función |
|---|---|---|
| GET | `/api/v1/categorias-producto` | Obtener categorías. |
| GET | `/api/v1/categorias-producto/{id}` | Obtener detalle. |
| POST | `/api/v1/categorias-producto` | Registrar categoría. |
| PUT | `/api/v1/categorias-producto/{id}` | Actualizar categoría. |
| DELETE | `/api/v1/categorias-producto/{id}` | Desactivar categoría. |
| GET | `/api/v1/categorias-producto/resumen` | Obtener resumen. |

Se utilizarán los mecanismos de comunicación con la API ya establecidos en el frontend.

No se duplicará la lógica de negocio del backend.

## 7. Estados de interfaz

Se contemplarán los siguientes estados:

- Cargando información.
- Listado con resultados.
- Listado vacío.
- Error de conexión con el backend.
- Registro exitoso.
- Actualización exitosa.
- Desactivación exitosa.
- Errores de validación.

Las acciones de desactivación solicitarán confirmación antes de ejecutarse.

## 8. Organización del módulo

La implementación se ubicará en:

```text
frontend/
└── src/
    └── modules/
        └── categorias-producto/
            ├── CategoriasProductoPage.tsx
            ├── categorias-producto.types.ts
            └── categorias-producto.api.ts
```

Se reutilizarán los componentes, estilos y mecanismos de navegación existentes.

La integración con el menú y las rutas respetará las convenciones del proyecto.

## 9. Criterios de aceptación

- [ ] La pantalla de categorías está implementada.
- [ ] Se visualiza correctamente el listado.
- [ ] La búsqueda funciona.
- [ ] Se muestra el resumen de categorías.
- [ ] Se pueden registrar categorías.
- [ ] Se pueden editar categorías.
- [ ] Se pueden desactivar categorías.
- [ ] Se muestran mensajes de éxito y error.
- [ ] Se validan los campos del formulario.
- [ ] La pantalla consume correctamente el backend.
- [ ] La navegación está integrada.
- [ ] Se mantiene el diseño visual del sistema.
- [ ] No se afecta el funcionamiento de otros módulos.

## 10. Evidencias

Se adjuntarán capturas de:

- Pantalla principal de categorías.
- Listado de categorías.
- Formulario de registro.
- Formulario de edición.
- Confirmación de desactivación.
- Visualización de estados y mensajes.
- Vista en resolución de 375 px.
- Vista en resolución de 1280 px.
- Tema claro y tema oscuro.

## 11. Restricciones

- No implementar lógica de negocio en el frontend.
- No modificar módulos de otros integrantes.
- No introducir dependencias innecesarias.
- No duplicar componentes existentes.
- No modificar estilos globales compartidos fuera de lo necesario y aprobado.
- No establecer relaciones entre categorías y productos desde el frontend.

## 12. Revisión y aprobación

La especificación será revisada por Lukas antes de iniciar la implementación.

Cualquier cambio de alcance será coordinado y aprobado antes de modificar el código.