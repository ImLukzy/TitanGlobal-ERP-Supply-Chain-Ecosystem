BEGIN;

TRUNCATE TABLE "RegistrosAuditoria" RESTART IDENTITY;

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
    ('OA-DEV-001', TIMESTAMPTZ '2026-03-12 09:00:00+00', 1, (SELECT "Id" FROM "SolicitudesCompra" WHERE "Numero" = 'SC-DEV-003'), 'Emitida', TIMESTAMPTZ '2099-12-31 00:00:00+00', NULL, 'Abastecimiento desde solicitud aprobada', NULL),
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
    (2, 'REC-DEV-002', TIMESTAMPTZ '2026-10-13 09:00:00+00', 1, 2, 5, 'Retraso', 'Media', 'Entrega de monitor demorada', 'EnProceso', TIMESTAMPTZ '2099-12-31 09:00:00+00', NULL, NULL, TIMESTAMPTZ '2026-10-14 09:00:00+00', NULL, NULL),
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

TRUNCATE TABLE "UsuariosRoles", "Usuarios" RESTART IDENTITY CASCADE;
TRUNCATE TABLE "PermisosRoles", "Roles" RESTART IDENTITY CASCADE;

INSERT INTO "Roles" ("Id", "Codigo", "Nombre", "Descripcion", "EsSistema") VALUES
    (1, 'ADMIN', 'Administrador', 'Acceso de sistema', TRUE),
    (2, 'VENTAS', 'Ventas', 'Gestión comercial', FALSE),
    (3, 'COMPRAS', 'Compras', 'Gestión de abastecimiento', FALSE),
    (4, 'ALMACEN', 'Almacén', 'Gestión de inventarios', FALSE),
    (5, 'CONSULTA', 'Consulta', 'Solo lectura', FALSE),
    (6, 'RRHH', 'Recursos Humanos', 'Gestión de capital humano', FALSE);

-- Contraseñas solo de desarrollo (hash BCrypt): admin → Admin2026!, invitado → Demo2026! (cuenta de demostración).
INSERT INTO "Usuarios" ("Id", "NombreUsuario", "NombreCompleto", "Email", "Activo", "FechaAlta", "ContrasenaHash") VALUES
    (1, 'admin', 'Administrador del sistema', 'admin@titanglobal.example', TRUE, TIMESTAMPTZ '2026-01-01 00:00:00+00', '$2a$11$d.nABDr1rIlogD5.EnGkh.r3dpHaNEluF.2zXgF1SRs5BD1ACgYva'),
    (2, 'ana.ventas', 'Ana Torres', 'ana.ventas@titanglobal.example', TRUE, TIMESTAMPTZ '2026-01-02 00:00:00+00', NULL),
    (3, 'luis.compras', 'Luis Rojas', 'luis.compras@titanglobal.example', TRUE, TIMESTAMPTZ '2026-01-03 00:00:00+00', NULL),
    (4, 'maria.almacen', 'María León', 'maria.almacen@titanglobal.example', FALSE, TIMESTAMPTZ '2026-01-04 00:00:00+00', NULL),
    (5, 'invitado', 'Usuario Invitado', 'invitado@titanglobal.example', TRUE, TIMESTAMPTZ '2026-01-05 00:00:00+00', '$2a$11$DWRsjvIcsNBeL9BRCdQsx.oy2vvO6p4UrphYNXmkgcQQzDFrngpAC');

INSERT INTO "UsuariosRoles" ("Id", "UsuarioId", "RolId") VALUES
    (1, 1, 1),
    (2, 2, 2),
    (3, 3, 3),
    (4, 3, 5),
    (5, 4, 4);

SELECT setval(pg_get_serial_sequence('"Usuarios"', 'Id'), (SELECT MAX("Id") FROM "Usuarios"));
SELECT setval(pg_get_serial_sequence('"UsuariosRoles"', 'Id'), (SELECT MAX("Id") FROM "UsuariosRoles"));

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
    (5, 80, TRUE, FALSE),
    (6, 61, TRUE, TRUE),
    (6, 62, TRUE, TRUE);

SELECT setval(pg_get_serial_sequence('"Roles"', 'Id'), (SELECT MAX("Id") FROM "Roles"));
SELECT setval(pg_get_serial_sequence('"PermisosRoles"', 'Id'), (SELECT MAX("Id") FROM "PermisosRoles"));

TRUNCATE TABLE "TasasImpuestos", "Impuestos" RESTART IDENTITY CASCADE;

INSERT INTO "Impuestos" ("Id", "Codigo", "Nombre", "Tipo", "Descripcion", "Pais", "Activo", "FechaCreacion") VALUES
    (1, 'IGV', 'Impuesto general a las ventas', 'IGV', 'IGV del 18 % sobre ventas gravadas', 'PER', TRUE, TIMESTAMPTZ '2026-01-01 00:00:00+00'),
    (2, 'RET-4TA', 'Retención de cuarta categoría', 'RETENCION', 'Retención sobre honorarios', 'PER', TRUE, TIMESTAMPTZ '2026-01-01 00:00:00+00');

INSERT INTO "TasasImpuestos" ("Id", "ImpuestoId", "Porcentaje", "FechaInicio", "FechaFin", "Activo", "Observacion") VALUES
    (1, 1, 18.000000, TIMESTAMPTZ '2020-01-01 00:00:00+00', NULL, TRUE, 'Tasa vigente del IGV'),
    (2, 2, 8.000000, TIMESTAMPTZ '2020-01-01 00:00:00+00', NULL, TRUE, 'Retención sobre honorarios');

SELECT setval(pg_get_serial_sequence('"Impuestos"', 'Id'), (SELECT MAX("Id") FROM "Impuestos"));
SELECT setval(pg_get_serial_sequence('"TasasImpuestos"', 'Id'), (SELECT MAX("Id") FROM "TasasImpuestos"));

INSERT INTO "CategoriasProducto" ("Id", "Nombre", "Descripcion", "Activa") VALUES
    (1, 'Computo', 'Laptops y equipos de cómputo', TRUE),
    (2, 'Perifericos', 'Mouse, teclados y accesorios de entrada', TRUE),
    (3, 'Pantallas', 'Monitores y pantallas', TRUE),
    (4, 'Descontinuados', NULL, FALSE)
ON CONFLICT ("Nombre") DO UPDATE SET
    "Descripcion" = EXCLUDED."Descripcion",
    "Activa" = EXCLUDED."Activa";

SELECT setval(pg_get_serial_sequence('"CategoriasProducto"', 'Id'), (SELECT MAX("Id") FROM "CategoriasProducto"));

INSERT INTO "CentrosTrabajo" (
    "Codigo", "Nombre", "Tipo", "Planta", "CapacidadHorasDia", "CostoHora", "Responsable", "Activo") VALUES
    ('CT-LIM-L01', 'Línea de envasado 1', 'Linea', 'Planta Lima', 16.00, 120.00, 'Carlos Ramos', TRUE),
    ('CT-LIM-L02', 'Línea de envasado 2', 'Linea', 'Planta Lima', 8.00, 110.00, 'Carlos Ramos', TRUE),
    ('CT-LIM-C01', 'Celda de ensamblaje', 'Celda', 'Planta Lima', 8.00, 85.50, 'Rosa Quispe', TRUE),
    ('CT-ARE-L01', 'Línea de mezclado', 'Linea', 'Planta Arequipa', 24.00, 150.00, 'Jorge Mamani', FALSE),
    ('CT-ARE-C01', 'Celda de etiquetado', 'Celda', 'Planta Arequipa', 12.00, 60.00, NULL, TRUE)
ON CONFLICT ("Codigo") DO UPDATE SET
    "Nombre" = EXCLUDED."Nombre",
    "Tipo" = EXCLUDED."Tipo",
    "Planta" = EXCLUDED."Planta",
    "CapacidadHorasDia" = EXCLUDED."CapacidadHorasDia",
    "CostoHora" = EXCLUDED."CostoHora",
    "Responsable" = EXCLUDED."Responsable",
    "Activo" = EXCLUDED."Activo";

SELECT setval(pg_get_serial_sequence('"CentrosTrabajo"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "CentrosTrabajo"));

INSERT INTO "VehiculosFlota" ("Id", "Placa", "Marca", "Modelo", "Anio", "Tipo", "CapacidadKg", "Activo", "Observaciones") VALUES
    (1, 'ABC-123', 'Volvo', 'FH16', 2022, 'Camion', 20000.00, TRUE, 'Unidad de larga distancia'),
    (2, 'DEF-456', 'Mercedes-Benz', 'Sprinter', 2021, 'Furgon', 1500.00, TRUE, NULL),
    (3, 'GHI-789', 'Scania', 'R450', 2023, 'Camion', 18000.00, TRUE, NULL),
    (4, 'JKL-012', 'Toyota', 'Hilux', 2020, 'Furgon', 1000.00, FALSE, 'Unidad en mantenimiento'),
    (5, 'MNO-345', 'Honda', 'CB500', 2022, 'Moto', 50.00, TRUE, 'Reparto urbano'),
    (6, 'PQR-678', 'Iveco', 'Daily', 2021, 'Otro', 3500.00, TRUE, NULL)
ON CONFLICT ("Placa") DO UPDATE SET
    "Marca" = EXCLUDED."Marca",
    "Modelo" = EXCLUDED."Modelo",
    "Anio" = EXCLUDED."Anio",
    "Tipo" = EXCLUDED."Tipo",
    "CapacidadKg" = EXCLUDED."CapacidadKg",
    "Activo" = EXCLUDED."Activo",
    "Observaciones" = EXCLUDED."Observaciones";

SELECT setval(pg_get_serial_sequence('"VehiculosFlota"', 'Id'), (SELECT MAX("Id") FROM "VehiculosFlota"));

INSERT INTO "Conductores" ("Id", "Nombre", "DocumentoLicencia", "VencimientoLicencia", "Habilitado", "Activo", "Observaciones") VALUES
    (1, 'Carlos Ruiz', 'PLT-0001', TIMESTAMPTZ '2027-12-31 00:00:00+00', TRUE, TRUE, NULL),
    (2, 'Maria Quispe', 'PLT-0002', TIMESTAMPTZ '2026-06-30 00:00:00+00', TRUE, TRUE, NULL),
    (3, 'Jose Flores', 'PLT-0003', TIMESTAMPTZ '2028-01-15 00:00:00+00', FALSE, TRUE, 'Sin habilitacion')
ON CONFLICT ("DocumentoLicencia") DO UPDATE SET
    "Nombre" = EXCLUDED."Nombre",
    "VencimientoLicencia" = EXCLUDED."VencimientoLicencia",
    "Habilitado" = EXCLUDED."Habilitado",
    "Activo" = EXCLUDED."Activo",
    "Observaciones" = EXCLUDED."Observaciones";

SELECT setval(pg_get_serial_sequence('"Conductores"', 'Id'), (SELECT MAX("Id") FROM "Conductores"));

TRUNCATE TABLE "Empleados" RESTART IDENTITY CASCADE;

INSERT INTO "Empleados" ("Id", "Legajo", "TipoDocumento", "NumeroDocumento", "Nombres", "Apellidos", "FechaNacimiento", "FechaIngreso", "Cargo", "Area", "Email", "Telefono", "Estado", "FechaCese", "MotivoCese") VALUES
    (1, 'EMP-000001', 'DNI', '10000001', 'Ana', 'Torres Vega', DATE '1990-05-14', DATE '2021-03-01', 'Gerente de Ventas', 'Ventas', 'ana.torres@titanglobal.example', '999000001', 'Activo', NULL, NULL),
    (2, 'EMP-000002', 'DNI', '10000002', 'Luis', 'Rojas Mendoza', DATE '1988-11-02', DATE '2019-08-16', 'Analista de Compras', 'Compras', 'luis.rojas@titanglobal.example', '999000002', 'Activo', NULL, NULL),
    (3, 'EMP-000003', 'CE', '004512367', 'Maria', 'Salazar Quispe', DATE '1995-02-20', DATE '2024-01-08', 'Almacenero', 'Almacen', 'maria.salazar@titanglobal.example', '999000003', 'Activo', NULL, NULL),
    (4, 'EMP-000004', 'Pasaporte', 'P1234567', 'Carlos', 'Diaz Soto', DATE '1982-07-30', DATE '2018-05-02', 'Auxiliar de Logistica', 'Logistica', NULL, NULL, 'Cesado', DATE '2026-09-30', 'Renuncia voluntaria');

SELECT setval(pg_get_serial_sequence('"Empleados"', 'Id'), (SELECT MAX("Id") FROM "Empleados"));

TRUNCATE TABLE "AdendasContrato", "ContratosLaborales" RESTART IDENTITY CASCADE;

INSERT INTO "ContratosLaborales" ("Id", "Numero", "EmpleadoId", "Modalidad", "FechaInicio", "FechaFin", "SueldoBasico", "Cargo", "Estado", "ContratoAnteriorId", "FechaCierre", "MotivoCierre") VALUES
    (1, 'CTR-000001', 1, 'Indeterminado', DATE '2021-03-01', NULL, 8500.00, 'Gerente de Ventas', 'Vigente', NULL, NULL, NULL),
    (2, 'CTR-000002', 2, 'PlazoFijo', DATE '2019-08-16', DATE '2020-08-15', 3200.00, 'Analista de Compras', 'Renovado', NULL, NULL, NULL),
    (3, 'CTR-000003', 2, 'PlazoFijo', DATE '2020-08-16', DATE '2021-08-15', 3400.00, 'Analista de Compras', 'Renovado', 2, NULL, NULL),
    (4, 'CTR-000004', 2, 'Indeterminado', DATE '2021-08-16', NULL, 4200.00, 'Analista de Compras', 'Vigente', 3, NULL, NULL),
    (5, 'CTR-000005', 3, 'Formativo', DATE '2024-01-08', DATE '2024-12-31', 1500.00, 'Almacenero', 'Vigente', NULL, NULL, NULL),
    (6, 'CTR-000006', 4, 'PlazoFijo', DATE '2024-05-02', DATE '2027-05-01', 2600.00, 'Auxiliar de Logistica', 'Cerrado', NULL, DATE '2026-09-30', 'Cese del empleado'),
    (7, 'CTR-000007', 1, 'PlazoFijo', DATE '2026-01-01', DATE '2026-12-31', 8500.00, 'Gerente de Ventas', 'Anulado', NULL, DATE '2026-01-05', 'Registro duplicado por error');

INSERT INTO "AdendasContrato" ("Id", "ContratoLaboralId", "FechaVigencia", "Cargo", "SueldoBasico") VALUES
    (1, 1, DATE '2024-01-01', 'Gerente Comercial', 9500.00);

SELECT setval(pg_get_serial_sequence('"ContratosLaborales"', 'Id'), (SELECT MAX("Id") FROM "ContratosLaborales"));
SELECT setval(pg_get_serial_sequence('"AdendasContrato"', 'Id'), (SELECT MAX("Id") FROM "AdendasContrato"));

TRUNCATE TABLE "AsignacionesTurno", "Turnos" RESTART IDENTITY CASCADE;

INSERT INTO "Turnos" ("Id", "Codigo", "Nombre", "Tipo", "HoraEntrada", "HoraSalida", "CruzaMedianoche", "ToleranciaMinutos", "DiasLaborables", "DiasTrabajo", "DiasDescanso", "FechaInicioCiclo", "Activo") VALUES
    (1, 'DIA', 'Turno dia', 'Fijo', TIME '08:00', TIME '17:00', false, 10, '1,2,3,4,5', NULL, NULL, NULL, true),
    (2, 'NOCHE', 'Turno noche', 'Fijo', TIME '22:00', TIME '06:00', true, 5, '1,2,3,4,5,6', NULL, NULL, NULL, true),
    (3, 'ROT4X2', 'Rotativo 4x2', 'Rotativo', TIME '07:00', TIME '19:00', false, 15, NULL, 4, 2, DATE '2026-01-05', true),
    (4, 'SABADO', 'Sabado medio dia', 'Fijo', TIME '08:00', TIME '13:00', false, 10, '6', NULL, NULL, NULL, false);

INSERT INTO "AsignacionesTurno" ("Id", "EmpleadoId", "TurnoId", "FechaInicio", "FechaFin") VALUES
    (1, 1, 1, DATE '2021-03-01', NULL),
    (2, 2, 2, DATE '2019-08-16', DATE '2025-12-31'),
    (3, 2, 3, DATE '2026-01-01', NULL),
    (4, 3, 1, DATE '2024-01-08', NULL),
    (5, 4, 1, DATE '2018-05-02', NULL);

SELECT setval(pg_get_serial_sequence('"Turnos"', 'Id'), (SELECT MAX("Id") FROM "Turnos"));
SELECT setval(pg_get_serial_sequence('"AsignacionesTurno"', 'Id'), (SELECT MAX("Id") FROM "AsignacionesTurno"));

TRUNCATE TABLE "JornadasAsistencia", "Marcaciones" RESTART IDENTITY CASCADE;

INSERT INTO "Marcaciones" ("Id", "EmpleadoId", "Tipo", "Instante", "Origen", "Dispositivo", "NumeroRegistro", "Observacion") VALUES
    (1, 1, 'Entrada', TIMESTAMPTZ '2026-11-02 13:05:00+00', 'Reloj', 'RELOJ-01', 'REG-0001', NULL),
    (2, 1, 'Salida', TIMESTAMPTZ '2026-11-02 22:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0002', NULL),
    (3, 1, 'Entrada', TIMESTAMPTZ '2026-11-03 13:25:00+00', 'Reloj', 'RELOJ-01', 'REG-0003', NULL),
    (4, 1, 'Salida', TIMESTAMPTZ '2026-11-03 22:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0004', NULL),
    (5, 1, 'Entrada', TIMESTAMPTZ '2026-11-04 13:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0005', NULL),
    (6, 1, 'Salida', TIMESTAMPTZ '2026-11-04 22:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0006', NULL),
    (7, 1, 'Entrada', TIMESTAMPTZ '2026-11-05 12:30:00+00', 'Reloj', 'RELOJ-01', 'REG-0007', NULL),
    (8, 1, 'Salida', TIMESTAMPTZ '2026-11-06 00:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0008', NULL),
    (9, 1, 'Entrada', TIMESTAMPTZ '2026-11-06 13:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0009', NULL),
    (10, 1, 'Salida', TIMESTAMPTZ '2026-11-06 22:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0010', NULL),
    (11, 2, 'Entrada', TIMESTAMPTZ '2026-11-02 12:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0011', NULL),
    (12, 2, 'Salida', TIMESTAMPTZ '2026-11-03 00:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0012', NULL),
    (13, 2, 'Entrada', TIMESTAMPTZ '2026-11-03 12:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0013', NULL),
    (14, 2, 'Salida', TIMESTAMPTZ '2026-11-04 00:00:00+00', 'Manual', NULL, NULL, 'Salida omitida por el reloj; confirmada por el jefe de turno'),
    (15, 2, 'Entrada', TIMESTAMPTZ '2026-11-04 12:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0014', NULL),
    (16, 2, 'Salida', TIMESTAMPTZ '2026-11-05 01:30:00+00', 'Reloj', 'RELOJ-01', 'REG-0015', NULL),
    (17, 3, 'Entrada', TIMESTAMPTZ '2026-11-02 13:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0016', NULL),
    (18, 3, 'Salida', TIMESTAMPTZ '2026-11-02 22:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0017', NULL),
    (19, 3, 'Entrada', TIMESTAMPTZ '2026-11-03 13:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0018', NULL),
    (20, 3, 'Salida', TIMESTAMPTZ '2026-11-03 22:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0019', NULL),
    (21, 3, 'Entrada', TIMESTAMPTZ '2026-11-04 13:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0020', NULL),
    (22, 3, 'Entrada', TIMESTAMPTZ '2026-11-05 13:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0021', NULL),
    (23, 3, 'Salida', TIMESTAMPTZ '2026-11-05 22:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0022', NULL),
    (24, 3, 'Entrada', TIMESTAMPTZ '2026-11-06 13:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0023', NULL),
    (25, 3, 'Salida', TIMESTAMPTZ '2026-11-06 22:00:00+00', 'Reloj', 'RELOJ-01', 'REG-0024', NULL);

INSERT INTO "JornadasAsistencia" ("Id", "EmpleadoId", "Fecha", "TurnoId", "HoraEntradaProgramada", "HoraSalidaProgramada", "ToleranciaMinutos", "Entrada", "Salida", "MinutosTrabajados", "MinutosTardanza", "MinutosExtra", "Estado", "EstadoHorasExtra", "DecididoPor", "FechaDecision", "MotivoRechazo") VALUES
    (1, 1, DATE '2026-11-02', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-02 13:05:00+00', TIMESTAMPTZ '2026-11-02 22:00:00+00', 535, 0, 0, 'Asistencia', 'NoAplica', NULL, NULL, NULL),
    (2, 1, DATE '2026-11-03', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-03 13:25:00+00', TIMESTAMPTZ '2026-11-03 22:00:00+00', 515, 15, 0, 'Tardanza', 'NoAplica', NULL, NULL, NULL),
    (3, 1, DATE '2026-11-04', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-04 13:00:00+00', TIMESTAMPTZ '2026-11-04 22:00:00+00', 540, 0, 0, 'Asistencia', 'NoAplica', NULL, NULL, NULL),
    (4, 1, DATE '2026-11-05', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-05 12:30:00+00', TIMESTAMPTZ '2026-11-06 00:00:00+00', 690, 0, 150, 'Asistencia', 'Aprobada', 'admin', TIMESTAMPTZ '2026-11-09 15:00:00+00', NULL),
    (5, 1, DATE '2026-11-06', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-06 13:00:00+00', TIMESTAMPTZ '2026-11-06 22:00:00+00', 540, 0, 0, 'Asistencia', 'NoAplica', NULL, NULL, NULL),
    (6, 2, DATE '2026-11-02', 3, TIME '07:00', TIME '19:00', 15, TIMESTAMPTZ '2026-11-02 12:00:00+00', TIMESTAMPTZ '2026-11-03 00:00:00+00', 720, 0, 0, 'Asistencia', 'NoAplica', NULL, NULL, NULL),
    (7, 2, DATE '2026-11-03', 3, TIME '07:00', TIME '19:00', 15, TIMESTAMPTZ '2026-11-03 12:00:00+00', TIMESTAMPTZ '2026-11-04 00:00:00+00', 720, 0, 0, 'Asistencia', 'NoAplica', NULL, NULL, NULL),
    (8, 2, DATE '2026-11-04', 3, TIME '07:00', TIME '19:00', 15, TIMESTAMPTZ '2026-11-04 12:00:00+00', TIMESTAMPTZ '2026-11-05 01:30:00+00', 810, 0, 90, 'Asistencia', 'Pendiente', NULL, NULL, NULL),
    (9, 3, DATE '2026-11-02', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-02 13:00:00+00', TIMESTAMPTZ '2026-11-02 22:00:00+00', 540, 0, 0, 'Asistencia', 'NoAplica', NULL, NULL, NULL),
    (10, 3, DATE '2026-11-03', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-03 13:00:00+00', TIMESTAMPTZ '2026-11-03 22:00:00+00', 540, 0, 0, 'Asistencia', 'NoAplica', NULL, NULL, NULL),
    (11, 3, DATE '2026-11-04', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-04 13:00:00+00', NULL, 0, 0, 0, 'SinMarcacion', 'NoAplica', NULL, NULL, NULL),
    (12, 3, DATE '2026-11-05', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-05 13:00:00+00', TIMESTAMPTZ '2026-11-05 22:00:00+00', 540, 0, 0, 'Asistencia', 'NoAplica', NULL, NULL, NULL),
    (13, 3, DATE '2026-11-06', 1, TIME '08:00', TIME '17:00', 10, TIMESTAMPTZ '2026-11-06 13:00:00+00', TIMESTAMPTZ '2026-11-06 22:00:00+00', 540, 0, 0, 'Asistencia', 'NoAplica', NULL, NULL, NULL);

SELECT setval(pg_get_serial_sequence('"Marcaciones"', 'Id'), (SELECT MAX("Id") FROM "Marcaciones"));
SELECT setval(pg_get_serial_sequence('"JornadasAsistencia"', 'Id'), (SELECT MAX("Id") FROM "JornadasAsistencia"));

TRUNCATE TABLE "Ausencias" RESTART IDENTITY CASCADE;

INSERT INTO "Ausencias" ("Id", "Numero", "EmpleadoId", "Tipo", "FechaInicio", "FechaFin", "Dias", "Motivo", "Estado", "FechaSolicitud", "DecididoPor", "FechaDecision", "MotivoRechazo", "FechaCancelacion") VALUES
    (1, 'AUS-000001', 1, 'Vacaciones', DATE '2026-02-02', DATE '2026-02-13', 12, NULL, 'Aprobada', DATE '2026-01-12', 'admin', TIMESTAMPTZ '2026-01-13 15:00:00+00', NULL, NULL),
    (2, 'AUS-000002', 1, 'Vacaciones', DATE '2027-01-04', DATE '2027-01-10', 7, NULL, 'Aprobada', DATE '2026-11-02', 'admin', TIMESTAMPTZ '2026-11-03 15:00:00+00', NULL, NULL),
    (3, 'AUS-000003', 1, 'PermisoConGoce', DATE '2026-12-14', DATE '2026-12-15', 2, 'Tramite notarial', 'Pendiente', DATE '2026-11-16', NULL, NULL, NULL, NULL),
    (4, 'AUS-000004', 2, 'DescansoMedico', DATE '2026-11-09', DATE '2026-11-13', 5, 'Cuadro gripal con certificado medico', 'Aprobada', DATE '2026-11-09', 'admin', TIMESTAMPTZ '2026-11-09 16:00:00+00', NULL, NULL),
    (5, 'AUS-000005', 2, 'PermisoSinGoce', DATE '2026-11-16', DATE '2026-11-17', 2, 'Asunto familiar', 'Aprobada', DATE '2026-11-10', 'admin', TIMESTAMPTZ '2026-11-11 15:00:00+00', NULL, NULL),
    (6, 'AUS-000006', 3, 'Vacaciones', DATE '2026-12-21', DATE '2026-12-31', 11, NULL, 'Rechazada', DATE '2026-11-05', 'admin', TIMESTAMPTZ '2026-11-06 15:00:00+00', 'Cierre de inventario anual', NULL),
    (7, 'AUS-000007', 3, 'Vacaciones', DATE '2027-02-01', DATE '2027-02-05', 5, NULL, 'Cancelada', DATE '2026-11-05', NULL, NULL, NULL, DATE '2026-11-10');

SELECT setval(pg_get_serial_sequence('"Ausencias"', 'Id'), (SELECT MAX("Id") FROM "Ausencias"));

TRUNCATE TABLE "DescuentosCuotaPrestamo", "CuotasPrestamo", "Prestamos" RESTART IDENTITY CASCADE;

INSERT INTO "Prestamos" ("Id", "Numero", "EmpleadoId", "Tipo", "Monto", "NumeroCuotas", "FechaOtorgamiento", "PrimerAnio", "PrimerMes", "Motivo", "Estado", "Saldo", "FechaAnulacion", "MotivoAnulacion") VALUES
    (1, 'PRE-000001', 1, 'Prestamo', 600.00, 6, DATE '2026-09-01', 2026, 9, 'Compra de equipo de computo', 'Activo', 400.00, NULL, NULL),
    (2, 'PRE-000002', 2, 'Adelanto', 500.00, 1, DATE '2026-10-05', 2026, 10, 'Gastos medicos', 'Cancelado', 0.00, NULL, NULL),
    (3, 'PRE-000003', 3, 'Prestamo', 300.00, 3, DATE '2026-10-20', 2026, 11, 'Curso de capacitacion', 'Anulado', 0.00, DATE '2026-10-22', 'Registrado por error');

INSERT INTO "CuotasPrestamo" ("Id", "PrestamoId", "NumeroCuota", "Anio", "Mes", "Monto", "MontoDescontado", "Estado") VALUES
    (1, 1, 1, 2026, 9, 100.00, 100.00, 'Descontada'),
    (2, 1, 2, 2026, 10, 100.00, 100.00, 'Descontada'),
    (3, 1, 3, 2026, 11, 100.00, 0.00, 'Pendiente'),
    (4, 1, 4, 2026, 12, 100.00, 0.00, 'Pendiente'),
    (5, 1, 5, 2027, 1, 100.00, 0.00, 'Pendiente'),
    (6, 1, 6, 2027, 2, 100.00, 0.00, 'Pendiente'),
    (7, 2, 1, 2026, 10, 500.00, 500.00, 'Descontada'),
    (8, 3, 1, 2026, 11, 100.00, 0.00, 'Pendiente'),
    (9, 3, 2, 2026, 12, 100.00, 0.00, 'Pendiente'),
    (10, 3, 3, 2027, 1, 100.00, 0.00, 'Pendiente');

SELECT setval(pg_get_serial_sequence('"Prestamos"', 'Id'), (SELECT MAX("Id") FROM "Prestamos"));
SELECT setval(pg_get_serial_sequence('"CuotasPrestamo"', 'Id'), (SELECT MAX("Id") FROM "CuotasPrestamo"));

TRUNCATE TABLE "DescuentosCuotaPrestamo", "BonosPlanilla", "PlanillaDetalles", "Planillas", "AfiliacionesPension", "ParametrosPension", "ParametrosPlanilla", "TramosQuintaCategoria" RESTART IDENTITY CASCADE;

INSERT INTO "ParametrosPlanilla" ("Id", "Clave", "Valor", "Descripcion") VALUES
    (1, 'UIT', 5350.0000, 'Unidad impositiva tributaria'),
    (2, 'TASA_ESSALUD', 0.0900, 'Aporte del empleador a EsSalud'),
    (3, 'HORAS_JORNADA', 8.0000, 'Horas de la jornada ordinaria'),
    (4, 'MINUTOS_RECARGO_25', 120.0000, 'Minutos de cada jornada con recargo del 25 por ciento'),
    (5, 'RECARGO_EXTRA_25', 0.2500, 'Recargo de los primeros minutos extra'),
    (6, 'RECARGO_EXTRA_35', 0.3500, 'Recargo del resto de minutos extra'),
    (7, 'DEDUCCION_UIT_QUINTA', 7.0000, 'UIT deducibles en la quinta categoria');

INSERT INTO "ParametrosPension" ("Id", "Codigo", "Sistema", "TasaAporte", "TasaPrimaSeguro", "TasaComision", "RemuneracionMaximaAsegurable") VALUES
    (1, 'ONP', 'ONP', 0.1300, 0.0000, 0.0000, NULL),
    (2, 'INTEGRA', 'AFP', 0.1000, 0.0137, 0.0155, 12027.10),
    (3, 'PRIMA', 'AFP', 0.1000, 0.0137, 0.0160, 12027.10),
    (4, 'PROFUTURO', 'AFP', 0.1000, 0.0137, 0.0169, 12027.10),
    (5, 'HABITAT', 'AFP', 0.1000, 0.0137, 0.0147, 12027.10);

INSERT INTO "TramosQuintaCategoria" ("Id", "Orden", "HastaUit", "Tasa") VALUES
    (1, 1, 5.00, 0.0800),
    (2, 2, 20.00, 0.1400),
    (3, 3, 35.00, 0.1700),
    (4, 4, 45.00, 0.2000),
    (5, 5, NULL, 0.3000);

INSERT INTO "AfiliacionesPension" ("Id", "EmpleadoId", "ParametroPensionId", "Cuspp", "FechaAfiliacion") VALUES
    (1, 1, 2, '123456ATORR1', DATE '2021-03-01'),
    (2, 2, 1, NULL, DATE '2019-08-16'),
    (3, 3, 3, '654321MSALA0', DATE '2024-01-08');

-- Planilla 2026-10 pagada: los importes salen de CalculadoraRemuneracion con estos datos (empleado 1: 9500 + bono 500, INTEGRA;
-- empleado 2: 4200, ONP; cuotas de prestamo descontadas de los prestamos 1 y 2 del modulo 67). Planilla 2026-11 en borrador.
INSERT INTO "Planillas" ("Id", "Numero", "Anio", "Mes", "Tipo", "Estado", "Empleados", "Incidencias", "TotalIngresos", "TotalDescuentos", "TotalNeto", "TotalAportesEmpleador", "FechaCalculo", "CalculadoPor", "AprobadoPor", "FechaAprobacion", "FechaPago") VALUES
    (1, 'PLA-202610-M', 2026, 10, 'Mensual', 'Pagada', 2, 0, 14200.00, 3353.66, 10846.34, 1278.00, TIMESTAMPTZ '2026-11-02 15:00:00+00', 'admin', 'admin', TIMESTAMPTZ '2026-11-03 15:00:00+00', DATE '2026-11-05'),
    (2, 'PLA-202611-M', 2026, 11, 'Mensual', 'Borrador', 0, 0, 0.00, 0.00, 0.00, 0.00, NULL, NULL, NULL, NULL, NULL);

INSERT INTO "PlanillaDetalles" ("Id", "PlanillaId", "EmpleadoId", "ContratoLaboralId", "Cargo", "SueldoBasico", "DiasContrato", "DiasSinGoce", "DiasFalta", "DiasLaborados", "IngresoBasico", "MinutosExtra", "IngresoHorasExtra", "IngresoBonos", "TotalIngresos", "CodigoPension", "DescuentoPension", "DescuentoQuinta", "DescuentoPrestamos", "TotalDescuentos", "Neto", "AporteEssalud", "Incidencia", "NumeroBoleta") VALUES
    (1, 1, 1, 1, 'Gerente Comercial', 9500.00, 30, 0, 0, 30, 9500.00, 0, 0.00, 500.00, 10000.00, 'INTEGRA', 1292.00, 829.33, 100.00, 2221.33, 7778.67, 900.00, NULL, 'BOL-202610-M-000001'),
    (2, 1, 2, 4, 'Analista de Compras', 4200.00, 30, 0, 0, 30, 4200.00, 0, 0.00, 0.00, 4200.00, 'ONP', 546.00, 86.33, 500.00, 1132.33, 3067.67, 378.00, NULL, 'BOL-202610-M-000002');

INSERT INTO "BonosPlanilla" ("Id", "EmpleadoId", "Anio", "Mes", "TipoPlanilla", "Concepto", "Monto", "PlanillaId") VALUES
    (1, 1, 2026, 10, 'Mensual', 'Bono por metas', 500.00, 1);

INSERT INTO "DescuentosCuotaPrestamo" ("Id", "CuotaPrestamoId", "PlanillaId", "Monto") VALUES
    (1, 2, 1, 100.00),
    (2, 7, 1, 500.00);

SELECT setval(pg_get_serial_sequence('"ParametrosPlanilla"', 'Id'), (SELECT MAX("Id") FROM "ParametrosPlanilla"));
SELECT setval(pg_get_serial_sequence('"ParametrosPension"', 'Id'), (SELECT MAX("Id") FROM "ParametrosPension"));
SELECT setval(pg_get_serial_sequence('"TramosQuintaCategoria"', 'Id'), (SELECT MAX("Id") FROM "TramosQuintaCategoria"));
SELECT setval(pg_get_serial_sequence('"AfiliacionesPension"', 'Id'), (SELECT MAX("Id") FROM "AfiliacionesPension"));
SELECT setval(pg_get_serial_sequence('"Planillas"', 'Id'), (SELECT MAX("Id") FROM "Planillas"));
SELECT setval(pg_get_serial_sequence('"PlanillaDetalles"', 'Id'), (SELECT MAX("Id") FROM "PlanillaDetalles"));
SELECT setval(pg_get_serial_sequence('"BonosPlanilla"', 'Id'), (SELECT MAX("Id") FROM "BonosPlanilla"));
SELECT setval(pg_get_serial_sequence('"DescuentosCuotaPrestamo"', 'Id'), (SELECT MAX("Id") FROM "DescuentosCuotaPrestamo"));

COMMIT;
