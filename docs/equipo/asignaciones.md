# Asignaciones de módulos

Antes de empezar un módulo, apúntate aquí (PR pequeño o avisando a Lukas). Un módulo lo lleva una sola persona de principio a fin (backend y pantalla), para no tocar los mismos archivos. Orden sugerido: el de la lista de `docs/arquitectura.md`, empezando por los que se apoyan en módulos ya hechos.

## Equipo
| Persona | Rol |
|---|---|
| Lukas Melgar | integrador: revisa specs y PR, integra en `ImLukzy/Claudio` y despliega la demo |
| Leonel Portillo | desarrollo de módulos |
| Antony Huayhua | desarrollo de módulos |
| Kelvin Pinto | desarrollo de módulos |

## Módulos
| Módulo | Responsable | Estado | Specs |
|---|---|---|---|
| 04 OrdenesCompra | Lukas (equipo Claude) | terminado | 01–06 |
| 01 Clientes | Lukas (equipo Claude) | terminado | 07, 08 |
| 21 Productos | Lukas (equipo Claude) | terminado | 09, 10 |
| 11 Proveedores | Lukas (equipo Claude) | terminado | 12, 13 |
| 13 SolicitudesCompra | Lukas (equipo Claude) | terminado | 14, 15 |
| 15 OrdenesAbastecimiento | Lukas (equipo Claude) | terminado | 16, 17, 18 |
| 12 EvaluacionProveedores | Leonel Portillo | asignado | — |
| 22 CategoriasProducto | Antony Huayhua | asignado (maestro aparte: no cambia la entidad `Producto` del enunciado) | — |
| 23 Almacenes | Lukas (equipo Claude) | backend terminado, pantalla en curso | 19, 20 |
| 24 ZonasAlmacenaje | Lukas (equipo Claude) | en curso | 21, 22 |
| 19 RecepcionMercaderia | Kelvin Pinto | asignado (depende de 15, terminado, y 23, en curso: empezar por la spec) | — |

Para los demás módulos, añade una fila al apuntarte. Primer paso de cada persona: escribir la spec de su módulo (ver `docs/guia-companeros.md` §4) y pedir a Lukas que la revise antes de programar.
