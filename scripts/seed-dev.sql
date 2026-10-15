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

COMMIT;
