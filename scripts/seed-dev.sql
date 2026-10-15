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

INSERT INTO "ListasPrecios" ("Codigo", "Nombre", "SegmentoClienteId", "Prioridad", "FechaInicio", "FechaFin", "Activa") VALUES
    ('VIP-2026', 'Precios VIP 2026', (SELECT "Id" FROM "SegmentosClientes" WHERE "Codigo" = 'VIP'), 1, TIMESTAMPTZ '2026-01-01 00:00:00+00', TIMESTAMPTZ '2026-12-31 00:00:00+00', TRUE),
    ('GENERAL-2026', 'Precios generales 2026', NULL, 10, TIMESTAMPTZ '2026-01-01 00:00:00+00', NULL, TRUE),
    ('PROMO-2025', 'Promoción 2025', NULL, 5, TIMESTAMPTZ '2025-01-01 00:00:00+00', TIMESTAMPTZ '2025-12-31 00:00:00+00', TRUE)
ON CONFLICT ("Codigo") DO UPDATE SET
    "Nombre" = EXCLUDED."Nombre",
    "SegmentoClienteId" = EXCLUDED."SegmentoClienteId",
    "Prioridad" = EXCLUDED."Prioridad",
    "FechaInicio" = EXCLUDED."FechaInicio",
    "FechaFin" = EXCLUDED."FechaFin",
    "Activa" = EXCLUDED."Activa";

DELETE FROM "ItemsListaPrecios"
WHERE "ListaPreciosId" IN (SELECT "Id" FROM "ListasPrecios" WHERE "Codigo" IN ('VIP-2026', 'GENERAL-2026', 'PROMO-2025'));

INSERT INTO "ItemsListaPrecios" ("ListaPreciosId", "ProductoId", "Precio") VALUES
    ((SELECT "Id" FROM "ListasPrecios" WHERE "Codigo" = 'VIP-2026'), 1, 2300.00),
    ((SELECT "Id" FROM "ListasPrecios" WHERE "Codigo" = 'VIP-2026'), 5, 820.00),
    ((SELECT "Id" FROM "ListasPrecios" WHERE "Codigo" = 'GENERAL-2026'), 1, 2450.00),
    ((SELECT "Id" FROM "ListasPrecios" WHERE "Codigo" = 'GENERAL-2026'), 3, 45.00),
    ((SELECT "Id" FROM "ListasPrecios" WHERE "Codigo" = 'PROMO-2025'), 1, 2000.00);

DELETE FROM "DetallesCotizacion";
TRUNCATE TABLE "Cotizaciones" RESTART IDENTITY CASCADE;

INSERT INTO "Cotizaciones" (
    "Id", "Numero", "Fecha", "ClienteId", "FechaValidez", "Estado", "Observaciones", "MotivoRechazo", "FechaDecision", "OrdenCompraId") VALUES
    (1, 'COT-DEV-001', TIMESTAMPTZ '2026-10-15 09:00:00+00', 1, TIMESTAMPTZ '2027-01-31 00:00:00+00', 'Emitida', 'Cotización vigente', NULL, NULL, NULL),
    (2, 'COT-DEV-002', TIMESTAMPTZ '2026-08-10 09:00:00+00', 2, TIMESTAMPTZ '2026-09-30 00:00:00+00', 'Emitida', NULL, NULL, NULL, NULL),
    (3, 'COT-DEV-003', TIMESTAMPTZ '2026-08-20 09:00:00+00', 3, TIMESTAMPTZ '2026-11-30 00:00:00+00', 'Rechazada', NULL, 'Presupuesto insuficiente', TIMESTAMPTZ '2026-08-21 09:00:00+00', NULL),
    (4, 'COT-DEV-004', TIMESTAMPTZ '2026-09-01 09:00:00+00', 3, TIMESTAMPTZ '2026-12-31 00:00:00+00', 'Aceptada', NULL, NULL, TIMESTAMPTZ '2026-09-02 09:00:00+00', NULL);

INSERT INTO "DetallesCotizacion" (
    "CotizacionId", "ProductoId", "Cantidad", "PrecioUnitario", "DescuentoPorcentaje") VALUES
    ((SELECT "Id" FROM "Cotizaciones" WHERE "Numero" = 'COT-DEV-001'), 1, 2, 100.00, 10.00),
    ((SELECT "Id" FROM "Cotizaciones" WHERE "Numero" = 'COT-DEV-002'), 2, 1, 50.00, 0.00),
    ((SELECT "Id" FROM "Cotizaciones" WHERE "Numero" = 'COT-DEV-003'), 3, 1, 100.00, 0.00),
    ((SELECT "Id" FROM "Cotizaciones" WHERE "Numero" = 'COT-DEV-004'), 4, 1, 80.00, 0.00);

SELECT setval(pg_get_serial_sequence('"Cotizaciones"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Cotizaciones"));
SELECT setval(pg_get_serial_sequence('"DetallesCotizacion"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "DetallesCotizacion"));


TRUNCATE TABLE "ContratosComerciales" RESTART IDENTITY;

INSERT INTO "ContratosComerciales" (
    "Id", "Numero", "ClienteId", "Titulo", "FechaInicio", "FechaFin", "MontoComprometido", "CondicionPago", "Estado", "Observaciones", "MotivoCancelacion", "FechaActivacion", "FechaCierre") VALUES
    (1, 'CC-DEV-001', 1, 'Acuerdo marco Acme 2026', TIMESTAMPTZ '2026-01-01 00:00:00+00', TIMESTAMPTZ '2026-12-31 00:00:00+00', 20000.00, 'Credito30', 'Vigente', 'Contrato anual', NULL, TIMESTAMPTZ '2026-01-01 00:00:00+00', NULL),
    (2, 'CC-DEV-002', 2, 'Acuerdo Beta 2027', TIMESTAMPTZ '2027-01-01 00:00:00+00', TIMESTAMPTZ '2027-12-31 00:00:00+00', 8000.00, 'Contado', 'Borrador', NULL, NULL, NULL, NULL),
    (3, 'CC-DEV-003', 3, 'Acuerdo Gamma cancelado', TIMESTAMPTZ '2026-01-01 00:00:00+00', TIMESTAMPTZ '2026-12-31 00:00:00+00', 5000.00, 'Credito60', 'Cancelado', NULL, 'Acuerdo reemplazado', NULL, TIMESTAMPTZ '2026-01-05 00:00:00+00');

SELECT setval(pg_get_serial_sequence('"ContratosComerciales"', 'Id'), (SELECT MAX("Id") FROM "ContratosComerciales"));

TRUNCATE TABLE "Promociones" RESTART IDENTITY;

INSERT INTO "Promociones" (
    "Id", "Codigo", "Nombre", "Tipo", "ProductoId", "MontoMinimo", "PorcentajeDescuento", "SegmentoClienteId", "FechaInicio", "FechaFin", "Activa") VALUES
    (1, 'LAPTOP-10', 'Laptop 10 %', 'Producto', 1, NULL, 10.00, NULL, TIMESTAMPTZ '2026-01-01 00:00:00+00', TIMESTAMPTZ '2026-12-31 00:00:00+00', TRUE),
    (2, 'LAPTOP-VIP-15', 'Laptop VIP 15 %', 'Producto', 1, NULL, 15.00, (SELECT "Id" FROM "SegmentosClientes" WHERE "Codigo" = 'VIP'), TIMESTAMPTZ '2026-01-01 00:00:00+00', TIMESTAMPTZ '2026-12-31 00:00:00+00', TRUE),
    (3, 'COMPRA-3000', 'Compra desde 3000', 'MontoMinimo', NULL, 3000.00, 5.00, NULL, TIMESTAMPTZ '2026-01-01 00:00:00+00', TIMESTAMPTZ '2026-12-31 00:00:00+00', TRUE),
    (4, 'VERANO-2025', 'Mouse de verano', 'Producto', 3, NULL, 20.00, NULL, TIMESTAMPTZ '2025-01-01 00:00:00+00', TIMESTAMPTZ '2025-12-31 00:00:00+00', TRUE);

SELECT setval(pg_get_serial_sequence('"Promociones"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Promociones"));

TRUNCATE TABLE "DetallesDevolucion", "DevolucionesComerciales", "ReclamosPostventa" RESTART IDENTITY;

INSERT INTO "ReclamosPostventa" (
    "Id", "Numero", "Fecha", "ClienteId", "OrdenCompraId", "ProductoId", "Tipo", "Prioridad", "Descripcion", "Estado", "FechaLimite", "Resolucion", "MotivoRechazo", "FechaInicio", "FechaResolucion", "FechaCierre") VALUES
    (1, 'REC-DEV-001', TIMESTAMPTZ '2026-10-01 09:00:00+00', 1, 1, 1, 'Defecto', 'Alta', 'Laptop con defecto de pantalla', 'Abierto', TIMESTAMPTZ '2026-10-03 09:00:00+00', NULL, NULL, NULL, NULL, NULL),
    (2, 'REC-DEV-002', TIMESTAMPTZ '2026-10-13 09:00:00+00', 1, 2, 5, 'Retraso', 'Media', 'Entrega de monitor demorada', 'EnProceso', TIMESTAMPTZ '2026-10-18 09:00:00+00', NULL, NULL, TIMESTAMPTZ '2026-10-14 09:00:00+00', NULL, NULL),
    (3, 'REC-DEV-003', TIMESTAMPTZ '2026-09-10 09:00:00+00', 2, 5, 3, 'Faltante', 'Media', 'Falta un accesorio en la entrega', 'Resuelto', TIMESTAMPTZ '2026-09-15 09:00:00+00', 'Se coordinó el envío del accesorio', NULL, TIMESTAMPTZ '2026-09-11 09:00:00+00', TIMESTAMPTZ '2026-09-12 09:00:00+00', NULL),
    (4, 'REC-DEV-004', TIMESTAMPTZ '2026-10-01 09:00:00+00', 3, 7, 8, 'Facturacion', 'Alta', 'Error en el comprobante de venta', 'Cerrado', TIMESTAMPTZ '2026-10-03 09:00:00+00', 'Se emitió el comprobante corregido', NULL, TIMESTAMPTZ '2026-10-02 09:00:00+00', TIMESTAMPTZ '2026-10-04 09:00:00+00', TIMESTAMPTZ '2026-10-05 09:00:00+00'),
    (5, 'REC-DEV-005', TIMESTAMPTZ '2026-09-01 09:00:00+00', 4, 8, 9, 'Otro', 'Baja', 'Consulta sobre garantía', 'Rechazado', TIMESTAMPTZ '2026-09-11 09:00:00+00', NULL, 'La consulta no corresponde a la compra', NULL, NULL, TIMESTAMPTZ '2026-09-02 09:00:00+00');

SELECT setval(pg_get_serial_sequence('"ReclamosPostventa"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "ReclamosPostventa"));

INSERT INTO "DevolucionesComerciales" (
    "Id", "Numero", "Fecha", "ClienteId", "OrdenCompraId", "ReclamoPostventaId", "Motivo", "Estado", "Observaciones", "MotivoRechazo", "FechaAprobacion", "FechaRecepcion", "FechaRechazo") VALUES
    (1, 'DEV-DEV-001', TIMESTAMPTZ '2026-10-15 08:00:00+00', 1, 1, 1, 'Defecto', 'Recibida', 'Laptop con falla', NULL, TIMESTAMPTZ '2026-10-15 09:00:00+00', TIMESTAMPTZ '2026-10-15 10:00:00+00', NULL),
    (2, 'DEV-DEV-002', TIMESTAMPTZ '2026-10-14 08:00:00+00', 1, 2, NULL, 'ErrorPedido', 'Aprobada', 'Monitor equivocado', NULL, TIMESTAMPTZ '2026-10-14 09:00:00+00', NULL, NULL),
    (3, 'DEV-DEV-003', TIMESTAMPTZ '2026-10-13 08:00:00+00', 2, 5, NULL, 'Arrepentimiento', 'Solicitada', NULL, NULL, NULL, NULL, NULL),
    (4, 'DEV-DEV-004', TIMESTAMPTZ '2026-10-12 08:00:00+00', 4, 8, NULL, 'Otro', 'Rechazada', 'Consulta de garantía', 'Fuera del plazo de devolución', NULL, NULL, TIMESTAMPTZ '2026-10-12 09:00:00+00');

INSERT INTO "DetallesDevolucion" ("DevolucionComercialId", "ProductoId", "Cantidad") VALUES
    (1, 1, 1),
    (2, 5, 1),
    (3, 3, 1),
    (4, 9, 1);

SELECT setval(pg_get_serial_sequence('"DevolucionesComerciales"', 'Id'), (SELECT MAX("Id") FROM "DevolucionesComerciales"));
SELECT setval(pg_get_serial_sequence('"DetallesDevolucion"', 'Id'), (SELECT MAX("Id") FROM "DetallesDevolucion"));

TRUNCATE TABLE "LiquidacionesComisiones", "AsignacionesVentas", "Vendedores" RESTART IDENTITY CASCADE;

INSERT INTO "Vendedores" ("Codigo", "Nombre", "Email", "PorcentajeComision", "Activo") VALUES
    ('V-ANA', 'Ana Torres', 'ana@titanglobal.example', 5.00, TRUE),
    ('V-LUIS', 'Luis Rojas', 'luis@titanglobal.example', 3.00, TRUE),
    ('V-OLD', 'Vendedor Antiguo', NULL, 4.00, FALSE);

INSERT INTO "AsignacionesVentas" ("OrdenCompraId", "VendedorId", "FechaAsignacion") VALUES
    (1, (SELECT "Id" FROM "Vendedores" WHERE "Codigo" = 'V-ANA'), TIMESTAMPTZ '2026-01-02 00:00:00+00'),
    (5, (SELECT "Id" FROM "Vendedores" WHERE "Codigo" = 'V-LUIS'), TIMESTAMPTZ '2026-01-15 00:00:00+00'),
    (2, (SELECT "Id" FROM "Vendedores" WHERE "Codigo" = 'V-ANA'), TIMESTAMPTZ '2026-02-05 00:00:00+00'),
    (4, (SELECT "Id" FROM "Vendedores" WHERE "Codigo" = 'V-LUIS'), TIMESTAMPTZ '2026-03-10 00:00:00+00');

INSERT INTO "LiquidacionesComisiones" (
    "VendedorId", "Anio", "Mes", "Ordenes", "Ventas", "Porcentaje", "Comision", "Estado", "FechaCalculo", "FechaPago") VALUES
    ((SELECT "Id" FROM "Vendedores" WHERE "Codigo" = 'V-ANA'), 2026, 1, 1, 2000.00, 5.00, 100.00, 'Pagada', TIMESTAMPTZ '2026-02-01 00:00:00+00', TIMESTAMPTZ '2026-02-05 00:00:00+00'),
    ((SELECT "Id" FROM "Vendedores" WHERE "Codigo" = 'V-LUIS'), 2026, 1, 1, 500.00, 3.00, 15.00, 'Pagada', TIMESTAMPTZ '2026-02-01 00:00:00+00', TIMESTAMPTZ '2026-02-05 00:00:00+00'),
    ((SELECT "Id" FROM "Vendedores" WHERE "Codigo" = 'V-ANA'), 2026, 2, 1, 3000.00, 5.00, 150.00, 'Calculada', TIMESTAMPTZ '2026-03-01 00:00:00+00', NULL);

SELECT setval(pg_get_serial_sequence('"Vendedores"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Vendedores"));
SELECT setval(pg_get_serial_sequence('"AsignacionesVentas"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "AsignacionesVentas"));
SELECT setval(pg_get_serial_sequence('"LiquidacionesComisiones"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "LiquidacionesComisiones"));

TRUNCATE TABLE "PermisosRoles", "Roles" RESTART IDENTITY CASCADE;

INSERT INTO "Roles" ("Id", "Codigo", "Nombre", "Descripcion", "EsSistema") VALUES
    (1, 'ADMIN', 'Administrador', 'Acceso de sistema', TRUE),
    (2, 'VENTAS', 'Ventas', 'Gestión comercial', FALSE),
    (3, 'COMPRAS', 'Compras', 'Gestión de abastecimiento', FALSE),
    (4, 'ALMACEN', 'Almacén', 'Gestión de inventarios', FALSE),
    (5, 'CONSULTA', 'Consulta', 'Solo lectura', FALSE);

INSERT INTO "PermisosRoles" ("RolId", "ModuloNumero", "PuedeLeer", "PuedeEscribir") VALUES
    (1, 1, TRUE, TRUE),
    (1, 2, TRUE, TRUE),
    (1, 3, TRUE, TRUE),
    (1, 4, TRUE, TRUE),
    (1, 5, TRUE, TRUE),
    (1, 6, TRUE, TRUE),
    (1, 7, TRUE, TRUE),
    (1, 8, TRUE, TRUE),
    (1, 9, TRUE, TRUE),
    (1, 10, TRUE, TRUE),
    (1, 11, TRUE, TRUE),
    (1, 12, TRUE, TRUE),
    (1, 13, TRUE, TRUE),
    (1, 14, TRUE, TRUE),
    (1, 15, TRUE, TRUE),
    (1, 16, TRUE, TRUE),
    (1, 17, TRUE, TRUE),
    (1, 18, TRUE, TRUE),
    (1, 19, TRUE, TRUE),
    (1, 20, TRUE, TRUE),
    (1, 21, TRUE, TRUE),
    (1, 22, TRUE, TRUE),
    (1, 23, TRUE, TRUE),
    (1, 24, TRUE, TRUE),
    (1, 25, TRUE, TRUE),
    (1, 26, TRUE, TRUE),
    (1, 27, TRUE, TRUE),
    (1, 28, TRUE, TRUE),
    (1, 29, TRUE, TRUE),
    (1, 30, TRUE, TRUE),
    (1, 31, TRUE, TRUE),
    (1, 32, TRUE, TRUE),
    (1, 33, TRUE, TRUE),
    (1, 34, TRUE, TRUE),
    (1, 35, TRUE, TRUE),
    (1, 36, TRUE, TRUE),
    (1, 37, TRUE, TRUE),
    (1, 38, TRUE, TRUE),
    (1, 39, TRUE, TRUE),
    (1, 40, TRUE, TRUE),
    (1, 41, TRUE, TRUE),
    (1, 42, TRUE, TRUE),
    (1, 43, TRUE, TRUE),
    (1, 44, TRUE, TRUE),
    (1, 45, TRUE, TRUE),
    (1, 46, TRUE, TRUE),
    (1, 47, TRUE, TRUE),
    (1, 48, TRUE, TRUE),
    (1, 49, TRUE, TRUE),
    (1, 50, TRUE, TRUE),
    (1, 51, TRUE, TRUE),
    (1, 52, TRUE, TRUE),
    (1, 53, TRUE, TRUE),
    (1, 54, TRUE, TRUE),
    (1, 55, TRUE, TRUE),
    (1, 56, TRUE, TRUE),
    (1, 57, TRUE, TRUE),
    (1, 58, TRUE, TRUE),
    (1, 59, TRUE, TRUE),
    (1, 60, TRUE, TRUE),
    (1, 61, TRUE, TRUE),
    (1, 62, TRUE, TRUE),
    (1, 63, TRUE, TRUE),
    (1, 64, TRUE, TRUE),
    (1, 65, TRUE, TRUE),
    (1, 66, TRUE, TRUE),
    (1, 67, TRUE, TRUE),
    (1, 68, TRUE, TRUE),
    (1, 69, TRUE, TRUE),
    (1, 70, TRUE, TRUE),
    (1, 71, TRUE, TRUE),
    (1, 72, TRUE, TRUE),
    (1, 73, TRUE, TRUE),
    (1, 74, TRUE, TRUE),
    (1, 75, TRUE, TRUE),
    (1, 76, TRUE, TRUE),
    (1, 77, TRUE, TRUE),
    (1, 78, TRUE, TRUE),
    (1, 79, TRUE, TRUE),
    (1, 80, TRUE, TRUE),
    (2, 1, TRUE, TRUE),
    (2, 2, TRUE, TRUE),
    (2, 3, TRUE, TRUE),
    (2, 4, TRUE, TRUE),
    (2, 5, TRUE, TRUE),
    (2, 6, TRUE, TRUE),
    (2, 7, TRUE, TRUE),
    (2, 8, TRUE, TRUE),
    (2, 9, TRUE, TRUE),
    (2, 10, TRUE, TRUE),
    (2, 21, TRUE, FALSE),
    (3, 11, TRUE, TRUE),
    (3, 12, TRUE, TRUE),
    (3, 13, TRUE, TRUE),
    (3, 14, TRUE, TRUE),
    (3, 15, TRUE, TRUE),
    (3, 16, TRUE, TRUE),
    (3, 17, TRUE, TRUE),
    (3, 18, TRUE, TRUE),
    (3, 19, TRUE, TRUE),
    (3, 20, TRUE, TRUE),
    (3, 21, TRUE, FALSE),
    (3, 22, TRUE, FALSE),
    (3, 23, TRUE, FALSE),
    (3, 24, TRUE, FALSE),
    (4, 11, TRUE, FALSE),
    (4, 12, TRUE, FALSE),
    (4, 13, TRUE, FALSE),
    (4, 14, TRUE, FALSE),
    (4, 15, TRUE, FALSE),
    (4, 16, TRUE, FALSE),
    (4, 17, TRUE, FALSE),
    (4, 18, TRUE, FALSE),
    (4, 19, TRUE, FALSE),
    (4, 20, TRUE, FALSE),
    (4, 21, TRUE, TRUE),
    (4, 22, TRUE, TRUE),
    (4, 23, TRUE, TRUE),
    (4, 24, TRUE, TRUE),
    (4, 25, TRUE, TRUE),
    (4, 26, TRUE, TRUE),
    (4, 27, TRUE, TRUE),
    (4, 28, TRUE, TRUE),
    (4, 29, TRUE, TRUE),
    (4, 30, TRUE, TRUE),
    (5, 1, TRUE, FALSE),
    (5, 2, TRUE, FALSE),
    (5, 3, TRUE, FALSE),
    (5, 4, TRUE, FALSE),
    (5, 5, TRUE, FALSE),
    (5, 6, TRUE, FALSE),
    (5, 7, TRUE, FALSE),
    (5, 8, TRUE, FALSE),
    (5, 9, TRUE, FALSE),
    (5, 10, TRUE, FALSE),
    (5, 11, TRUE, FALSE),
    (5, 12, TRUE, FALSE),
    (5, 13, TRUE, FALSE),
    (5, 14, TRUE, FALSE),
    (5, 15, TRUE, FALSE),
    (5, 16, TRUE, FALSE),
    (5, 17, TRUE, FALSE),
    (5, 18, TRUE, FALSE),
    (5, 19, TRUE, FALSE),
    (5, 20, TRUE, FALSE),
    (5, 21, TRUE, FALSE),
    (5, 22, TRUE, FALSE),
    (5, 23, TRUE, FALSE),
    (5, 24, TRUE, FALSE),
    (5, 25, TRUE, FALSE),
    (5, 26, TRUE, FALSE),
    (5, 27, TRUE, FALSE),
    (5, 28, TRUE, FALSE),
    (5, 29, TRUE, FALSE),
    (5, 30, TRUE, FALSE),
    (5, 31, TRUE, FALSE),
    (5, 32, TRUE, FALSE),
    (5, 33, TRUE, FALSE),
    (5, 34, TRUE, FALSE),
    (5, 35, TRUE, FALSE),
    (5, 36, TRUE, FALSE),
    (5, 37, TRUE, FALSE),
    (5, 38, TRUE, FALSE),
    (5, 39, TRUE, FALSE),
    (5, 40, TRUE, FALSE),
    (5, 41, TRUE, FALSE),
    (5, 42, TRUE, FALSE),
    (5, 43, TRUE, FALSE),
    (5, 44, TRUE, FALSE),
    (5, 45, TRUE, FALSE),
    (5, 46, TRUE, FALSE),
    (5, 47, TRUE, FALSE),
    (5, 48, TRUE, FALSE),
    (5, 49, TRUE, FALSE),
    (5, 50, TRUE, FALSE),
    (5, 51, TRUE, FALSE),
    (5, 52, TRUE, FALSE),
    (5, 53, TRUE, FALSE),
    (5, 54, TRUE, FALSE),
    (5, 55, TRUE, FALSE),
    (5, 56, TRUE, FALSE),
    (5, 57, TRUE, FALSE),
    (5, 58, TRUE, FALSE),
    (5, 59, TRUE, FALSE),
    (5, 60, TRUE, FALSE),
    (5, 61, TRUE, FALSE),
    (5, 62, TRUE, FALSE),
    (5, 63, TRUE, FALSE),
    (5, 64, TRUE, FALSE),
    (5, 65, TRUE, FALSE),
    (5, 66, TRUE, FALSE),
    (5, 67, TRUE, FALSE),
    (5, 68, TRUE, FALSE),
    (5, 69, TRUE, FALSE),
    (5, 70, TRUE, FALSE),
    (5, 71, TRUE, FALSE),
    (5, 72, TRUE, FALSE),
    (5, 73, TRUE, FALSE),
    (5, 74, TRUE, FALSE),
    (5, 75, TRUE, FALSE),
    (5, 76, TRUE, FALSE),
    (5, 77, TRUE, FALSE),
    (5, 78, TRUE, FALSE),
    (5, 79, TRUE, FALSE),
    (5, 80, TRUE, FALSE);

SELECT setval(pg_get_serial_sequence('"Roles"', 'Id'), (SELECT MAX("Id") FROM "Roles"));
SELECT setval(pg_get_serial_sequence('"PermisosRoles"', 'Id'), (SELECT MAX("Id") FROM "PermisosRoles"));

COMMIT;
