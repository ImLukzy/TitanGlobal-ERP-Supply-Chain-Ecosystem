BEGIN;

TRUNCATE TABLE "DetallesOrdenCompra", "OrdenesCompra", "Clientes", "Productos" RESTART IDENTITY CASCADE;

INSERT INTO "Clientes" ("Id", "Nombre", "Ruc", "Descripcion") VALUES
    (1, 'Acme Industrial', '20111111111', 'Cliente frecuente'),
    (2, 'Beta Comercio', '20222222222', NULL),
    (3, 'Gamma Manufactura', '20333333333', 'Cliente regional'),
    (4, 'Delta Logística', '20444444444', NULL),
    (5, 'Cliente sin órdenes', '20555555555', NULL);

INSERT INTO "Proveedores" ("Id", "RazonSocial", "Ruc", "Pais", "Email", "Telefono", "Activo") VALUES
    (1, 'Andes Suministros S.A.C.', '20600000001', 'Peru', 'ventas@andes.example', '+51 1 555-0101', TRUE),
    (2, 'Minerales del Sur S.A.C.', '20600000002', 'Peru', 'contacto@minerales.example', '01 555 0102', TRUE),
    (3, 'Logística Pacífico S.A.C.', '20600000003', 'Peru', NULL, NULL, TRUE),
    (4, 'Cobre Austral SpA', '76000000004', 'Chile', 'compras@cobreaustral.example', '+56 2 555-0104', TRUE),
    (5, 'Servicios Andinos SpA', '76000000005', 'Chile', NULL, '+56 2 555-0105', FALSE),
    (6, 'Shenzhen Parts Ltd.', '91000000006', 'China', 'sales@shenzhen.example', '+86 755 555 0106', TRUE)
ON CONFLICT ("Ruc") DO UPDATE SET
    "RazonSocial" = EXCLUDED."RazonSocial",
    "Pais" = EXCLUDED."Pais",
    "Email" = EXCLUDED."Email",
    "Telefono" = EXCLUDED."Telefono",
    "Activo" = EXCLUDED."Activo";

INSERT INTO "Productos" ("Id", "Nombre", "Categoria", "Precio", "Descripcion") VALUES
    (1, 'Laptop Pro', 'Computo', 2500.00, 'Laptop de alto rendimiento'),
    (2, 'Laptop Básica', 'Computo', 1500.00, 'Laptop de oficina'),
    (3, 'Mouse Óptico', 'Perifericos', 50.00, 'Mouse USB'),
    (4, 'Teclado Mecánico', 'Perifericos', 200.00, NULL),
    (5, 'Monitor 27', 'Pantallas', 900.00, 'Monitor QHD'),
    (6, 'Cable HDMI', 'Accesorios', 25.00, NULL),
    (7, 'Laptop Air', 'Computo', 3200.00, 'Laptop ligera'),
    (8, 'Mouse Gamer', 'Perifericos', 120.00, 'Mouse RGB'),
    (9, 'Silla Ejecutiva', 'Oficina', 800.00, NULL),
    (10, 'Monitor 24', 'Pantallas', 650.00, 'Monitor Full HD');

INSERT INTO "OrdenesCompra" ("Id", "NumeroOrden", "Fecha", "Total", "Descripcion", "ClienteId") VALUES
    (1, 'OC-001', TIMESTAMPTZ '2026-01-02 00:00:00+00', 2000.00, 'Equipamiento de cómputo', 1),
    (2, 'OC-002', TIMESTAMPTZ '2026-02-05 00:00:00+00', 3000.00, 'Monitores de oficina', 1),
    (3, 'OC-003', TIMESTAMPTZ '2025-11-01 00:00:00+00', 1000.00, 'Compra histórica', 1),
    (4, 'OC-004', TIMESTAMPTZ '2026-03-10 00:00:00+00', 4000.00, 'Accesorios adicionales', 1),
    (5, 'OC-005', TIMESTAMPTZ '2026-01-15 00:00:00+00', 500.00, 'Periféricos', 2),
    (6, 'OC-006', TIMESTAMPTZ '2025-12-01 00:00:00+00', 1500.00, 'Teclado de oficina', 2),
    (7, 'OC-007', TIMESTAMPTZ '2026-04-01 00:00:00+00', 500.00, 'Mouse para estación', 3),
    (8, 'OC-008', TIMESTAMPTZ '2026-05-01 00:00:00+00', 750.00, 'Silla para oficina', 4);

INSERT INTO "DetallesOrdenCompra" ("Id", "OrdenCompraId", "ProductoId", "Cantidad", "PrecioUnitario") VALUES
    (1, 1, 1, 1, 2500.00),
    (2, 1, 3, 2, 50.00),
    (3, 2, 5, 1, 900.00),
    (4, 3, 7, 1, 3200.00),
    (5, 4, 3, 1, 50.00),
    (6, 5, 3, 1, 50.00),
    (7, 6, 4, 1, 200.00),
    (8, 7, 8, 1, 120.00),
    (9, 8, 9, 1, 800.00);

SELECT setval(pg_get_serial_sequence('"Clientes"', 'Id'), (SELECT MAX("Id") FROM "Clientes"));
SELECT setval(pg_get_serial_sequence('"Proveedores"', 'Id'), (SELECT MAX("Id") FROM "Proveedores"));
SELECT setval(pg_get_serial_sequence('"Productos"', 'Id'), (SELECT MAX("Id") FROM "Productos"));
SELECT setval(pg_get_serial_sequence('"OrdenesCompra"', 'Id'), (SELECT MAX("Id") FROM "OrdenesCompra"));
SELECT setval(pg_get_serial_sequence('"DetallesOrdenCompra"', 'Id'), (SELECT MAX("Id") FROM "DetallesOrdenCompra"));

INSERT INTO "SolicitudesCompra" (
    "Numero", "Fecha", "Solicitante", "ProveedorId", "Estado", "Observaciones", "MotivoRechazo", "FechaDecision") VALUES
    ('SC-DEV-001', TIMESTAMPTZ '2026-01-10 09:00:00+00', 'Mantenimiento Planta', 1, 'Pendiente', 'Repuestos preventivos', NULL, NULL),
    ('SC-DEV-002', TIMESTAMPTZ '2026-02-15 14:30:00+00', 'Operaciones', NULL, 'Pendiente', 'Solicitud sin proveedor asignado', NULL, NULL),
    ('SC-DEV-003', TIMESTAMPTZ '2026-03-10 11:00:00+00', 'Producción', 1, 'Aprobada', 'Insumos de producción', NULL, TIMESTAMPTZ '2026-03-11 10:00:00+00'),
    ('SC-DEV-004', TIMESTAMPTZ '2026-04-20 08:00:00+00', 'Logística', 2, 'Aprobada', 'Equipos de almacén', NULL, TIMESTAMPTZ '2026-04-21 08:00:00+00'),
    ('SC-DEV-005', TIMESTAMPTZ '2026-05-01 12:00:00+00', 'Taller', 1, 'Rechazada', 'Herramientas especiales', 'Fuera del presupuesto aprobado', TIMESTAMPTZ '2026-05-02 12:00:00+00')
ON CONFLICT ("Numero") DO UPDATE SET
    "Fecha" = EXCLUDED."Fecha",
    "Solicitante" = EXCLUDED."Solicitante",
    "ProveedorId" = EXCLUDED."ProveedorId",
    "Estado" = EXCLUDED."Estado",
    "Observaciones" = EXCLUDED."Observaciones",
    "MotivoRechazo" = EXCLUDED."MotivoRechazo",
    "FechaDecision" = EXCLUDED."FechaDecision";

DELETE FROM "DetallesSolicitudCompra"
WHERE "SolicitudCompraId" IN (
    SELECT "Id" FROM "SolicitudesCompra" WHERE "Numero" IN (
        'SC-DEV-001', 'SC-DEV-002', 'SC-DEV-003', 'SC-DEV-004', 'SC-DEV-005'));

INSERT INTO "DetallesSolicitudCompra" ("SolicitudCompraId", "ProductoId", "Cantidad", "PrecioEstimado") VALUES
    ((SELECT "Id" FROM "SolicitudesCompra" WHERE "Numero" = 'SC-DEV-001'), 1, 2, 2300.00),
    ((SELECT "Id" FROM "SolicitudesCompra" WHERE "Numero" = 'SC-DEV-001'), 3, 4, 40.00),
    ((SELECT "Id" FROM "SolicitudesCompra" WHERE "Numero" = 'SC-DEV-002'), 2, 10, 1200.00),
    ((SELECT "Id" FROM "SolicitudesCompra" WHERE "Numero" = 'SC-DEV-003'), 7, 3, 2900.00),
    ((SELECT "Id" FROM "SolicitudesCompra" WHERE "Numero" = 'SC-DEV-004'), 5, 2, 750.00),
    ((SELECT "Id" FROM "SolicitudesCompra" WHERE "Numero" = 'SC-DEV-005'), 3, 100, 45.00);

SELECT setval(pg_get_serial_sequence('"SolicitudesCompra"', 'Id'), (SELECT MAX("Id") FROM "SolicitudesCompra"));
SELECT setval(pg_get_serial_sequence('"DetallesSolicitudCompra"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "DetallesSolicitudCompra"));

TRUNCATE TABLE "DetallesOrdenesAbastecimiento", "OrdenesAbastecimiento" RESTART IDENTITY;

INSERT INTO "OrdenesAbastecimiento" (
    "Numero", "Fecha", "ProveedorId", "SolicitudCompraId", "Estado", "FechaEntregaEstimada", "FechaRecepcion", "Observaciones", "MotivoAnulacion") VALUES
    ('OA-DEV-001', TIMESTAMPTZ '2026-03-12 09:00:00+00', 1, (SELECT "Id" FROM "SolicitudesCompra" WHERE "Numero" = 'SC-DEV-003'), 'Emitida', TIMESTAMPTZ '2026-10-20 00:00:00+00', NULL, 'Abastecimiento desde solicitud aprobada', NULL),
    ('OA-DEV-002', TIMESTAMPTZ '2026-01-20 09:00:00+00', 2, NULL, 'Emitida', TIMESTAMPTZ '2026-02-01 00:00:00+00', NULL, 'Orden atrasada', NULL),
    ('OA-DEV-003', TIMESTAMPTZ '2026-02-20 09:00:00+00', 3, NULL, 'Recibida', TIMESTAMPTZ '2026-03-01 00:00:00+00', TIMESTAMPTZ '2026-02-28 12:00:00+00', NULL, NULL),
    ('OA-DEV-004', TIMESTAMPTZ '2026-04-20 09:00:00+00', 1, NULL, 'Anulada', TIMESTAMPTZ '2026-05-01 00:00:00+00', NULL, NULL, 'Orden duplicada');

INSERT INTO "DetallesOrdenesAbastecimiento" ("OrdenAbastecimientoId", "ProductoId", "Cantidad", "PrecioUnitario") VALUES
    ((SELECT "Id" FROM "OrdenesAbastecimiento" WHERE "Numero" = 'OA-DEV-001'), 7, 3, 2900.00),
    ((SELECT "Id" FROM "OrdenesAbastecimiento" WHERE "Numero" = 'OA-DEV-002'), 1, 1, 1000.00),
    ((SELECT "Id" FROM "OrdenesAbastecimiento" WHERE "Numero" = 'OA-DEV-003'), 3, 5, 50.00),
    ((SELECT "Id" FROM "OrdenesAbastecimiento" WHERE "Numero" = 'OA-DEV-004'), 5, 2, 10.00);

SELECT setval(pg_get_serial_sequence('"OrdenesAbastecimiento"', 'Id'), (SELECT MAX("Id") FROM "OrdenesAbastecimiento"));
SELECT setval(pg_get_serial_sequence('"DetallesOrdenesAbastecimiento"', 'Id'), (SELECT MAX("Id") FROM "DetallesOrdenesAbastecimiento"));

INSERT INTO "Almacenes" (
    "Codigo", "Nombre", "Tipo", "Direccion", "Ciudad", "CapacidadM3", "Responsable", "Activo") VALUES
    ('ALM-LIM-01', 'Central Lima', 'Central', 'Av. Industrial 100', 'Lima', 500.00, 'Ana Torres', TRUE),
    ('ALM-LIM-02', 'Regional Lima', 'Regional', 'Jr. Logística 200', 'Lima', 200.00, 'Luis Rojas', TRUE),
    ('ALM-ARE-01', 'Regional Arequipa', 'Regional', 'Av. Sur 300', 'Arequipa', 150.00, 'María León', TRUE),
    ('ALM-TRU-01', 'Regional Trujillo', 'Regional', 'Av. Norte 400', 'Trujillo', 100.00, 'Carlos Díaz', FALSE),
    ('ALM-CAL-TR', 'Tránsito Callao', 'Transito', 'Av. Puerto 500', 'Callao', 75.00, NULL, TRUE)
ON CONFLICT ("Codigo") DO UPDATE SET
    "Nombre" = EXCLUDED."Nombre",
    "Tipo" = EXCLUDED."Tipo",
    "Direccion" = EXCLUDED."Direccion",
    "Ciudad" = EXCLUDED."Ciudad",
    "CapacidadM3" = EXCLUDED."CapacidadM3",
    "Responsable" = EXCLUDED."Responsable",
    "Activo" = EXCLUDED."Activo";

SELECT setval(pg_get_serial_sequence('"Almacenes"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Almacenes"));

INSERT INTO "ZonasAlmacenaje" (
    "AlmacenId", "Codigo", "Nombre", "Tipo", "Temperatura", "CapacidadM3", "Activa") VALUES
    ((SELECT "Id" FROM "Almacenes" WHERE "Codigo" = 'ALM-LIM-01'), 'REC-01', 'Recepción principal', 'Recepcion', 'Ambiente', 50.00, TRUE),
    ((SELECT "Id" FROM "Almacenes" WHERE "Codigo" = 'ALM-LIM-01'), 'ALM-A', 'Almacenamiento A', 'Almacenamiento', 'Ambiente', 300.00, TRUE),
    ((SELECT "Id" FROM "Almacenes" WHERE "Codigo" = 'ALM-LIM-01'), 'PIC-01', 'Picking 01', 'Picking', 'Ambiente', 60.00, TRUE),
    ((SELECT "Id" FROM "Almacenes" WHERE "Codigo" = 'ALM-LIM-01'), 'DES-01', 'Despacho 01', 'Despacho', 'Ambiente', 40.00, TRUE),
    ((SELECT "Id" FROM "Almacenes" WHERE "Codigo" = 'ALM-ARE-01'), 'ALM-A', 'Almacenamiento A', 'Almacenamiento', 'Ambiente', 100.00, TRUE),
    ((SELECT "Id" FROM "Almacenes" WHERE "Codigo" = 'ALM-ARE-01'), 'FRIO-01', 'Pasillo frío', 'Almacenamiento', 'Refrigerada', 30.00, TRUE),
    ((SELECT "Id" FROM "Almacenes" WHERE "Codigo" = 'ALM-CAL-TR'), 'TRAN-01', 'Tránsito 01', 'Recepcion', 'Ambiente', 60.00, TRUE),
    ((SELECT "Id" FROM "Almacenes" WHERE "Codigo" = 'ALM-CAL-TR'), 'CUA-01', 'Cuarentena 01', 'Cuarentena', 'Ambiente', 20.00, FALSE)
ON CONFLICT ("AlmacenId", "Codigo") DO UPDATE SET
    "Nombre" = EXCLUDED."Nombre",
    "Tipo" = EXCLUDED."Tipo",
    "Temperatura" = EXCLUDED."Temperatura",
    "CapacidadM3" = EXCLUDED."CapacidadM3",
    "Activa" = EXCLUDED."Activa";

SELECT setval(pg_get_serial_sequence('"ZonasAlmacenaje"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "ZonasAlmacenaje"));

INSERT INTO "SegmentosClientes" (
    "Id", "Codigo", "Nombre", "Descripcion", "Prioridad", "MontoMinimo", "OrdenesMinimas", "Activo") VALUES
    (1, 'VIP', 'Cliente VIP', 'Criterio de alto valor', 1, 5000.00, 3, TRUE),
    (2, 'FRECUENTE', 'Cliente frecuente', NULL, 2, 0.00, 2, TRUE),
    (3, 'OCASIONAL', 'Cliente ocasional', NULL, 3, 0.00, 1, TRUE),
    (4, 'PREMIUM-OLD', 'Premium antiguo', NULL, 4, 1000.00, 1, FALSE)
ON CONFLICT ("Codigo") DO UPDATE SET
    "Nombre" = EXCLUDED."Nombre",
    "Descripcion" = EXCLUDED."Descripcion",
    "Prioridad" = EXCLUDED."Prioridad",
    "MontoMinimo" = EXCLUDED."MontoMinimo",
    "OrdenesMinimas" = EXCLUDED."OrdenesMinimas",
    "Activo" = EXCLUDED."Activo";

SELECT setval(pg_get_serial_sequence('"SegmentosClientes"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "SegmentosClientes"));

COMMIT;
