-- TitanGlobal ERP: reglas de integridad en la base de datos (spec 53).
--
-- Cada regla repite en la base una validación que la aplicación ya hace en todas sus rutas de escritura, así
-- que nunca rechaza un dato que la API acepte: es una segunda defensa ante datos cargados por fuera de la API.
-- Donde la aplicación valida "> 0" sin limitar decimales, la regla es ">= 0": PostgreSQL redondea a la escala
-- de la columna antes de comprobar (0,004 se guarda como 0,00).
-- Convención de nombres: CK_<Tabla>_<Regla>.

-- 1. Longitudes en las tablas del enunciado (validadas en ValidacionCliente y ValidacionProducto).
ALTER TABLE "Clientes"
    ALTER COLUMN "Nombre" TYPE character varying(200),
    ALTER COLUMN "Ruc" TYPE character varying(11),
    ADD CONSTRAINT "CK_Clientes_NombreNoVacio" CHECK (btrim("Nombre") <> ''),
    ADD CONSTRAINT "CK_Clientes_RucOnceDigitos" CHECK ("Ruc" ~ '^[0-9]{11}$');

ALTER TABLE "Productos"
    ALTER COLUMN "Nombre" TYPE character varying(200),
    ALTER COLUMN "Categoria" TYPE character varying(100),
    ADD CONSTRAINT "CK_Productos_NombreNoVacio" CHECK (btrim("Nombre") <> ''),
    ADD CONSTRAINT "CK_Productos_CategoriaNoVacia" CHECK (btrim("Categoria") <> ''),
    ADD CONSTRAINT "CK_Productos_PrecioNoNegativo" CHECK ("Precio" >= 0);

ALTER TABLE "OrdenesCompra"
    ADD CONSTRAINT "CK_OrdenesCompra_NumeroNoVacio" CHECK (btrim("NumeroOrden") <> ''),
    ADD CONSTRAINT "CK_OrdenesCompra_TotalNoNegativo" CHECK ("Total" >= 0);

ALTER TABLE "DetallesOrdenCompra"
    ADD CONSTRAINT "CK_DetallesOrdenCompra_CantidadPositiva" CHECK ("Cantidad" > 0),
    ADD CONSTRAINT "CK_DetallesOrdenCompra_PrecioNoNegativo" CHECK ("PrecioUnitario" >= 0);

-- 2. Comercial y ventas.
ALTER TABLE "SegmentosClientes"
    ADD CONSTRAINT "CK_SegmentosClientes_Prioridad" CHECK ("Prioridad" BETWEEN 1 AND 999),
    ADD CONSTRAINT "CK_SegmentosClientes_MontoMinimoNoNegativo" CHECK ("MontoMinimo" >= 0),
    ADD CONSTRAINT "CK_SegmentosClientes_OrdenesMinimasNoNegativas" CHECK ("OrdenesMinimas" >= 0);

ALTER TABLE "Cotizaciones"
    ADD CONSTRAINT "CK_Cotizaciones_Estado" CHECK ("Estado" IN ('Emitida', 'Aceptada', 'Rechazada'));

ALTER TABLE "DetallesCotizacion"
    ADD CONSTRAINT "CK_DetallesCotizacion_CantidadPositiva" CHECK ("Cantidad" > 0),
    ADD CONSTRAINT "CK_DetallesCotizacion_PrecioNoNegativo" CHECK ("PrecioUnitario" >= 0),
    ADD CONSTRAINT "CK_DetallesCotizacion_Descuento" CHECK ("DescuentoPorcentaje" BETWEEN 0 AND 100);

ALTER TABLE "ContratosComerciales"
    ADD CONSTRAINT "CK_ContratosComerciales_Estado" CHECK ("Estado" IN ('Borrador', 'Vigente', 'Finalizado', 'Cancelado')),
    ADD CONSTRAINT "CK_ContratosComerciales_MontoNoNegativo" CHECK ("MontoComprometido" >= 0),
    ADD CONSTRAINT "CK_ContratosComerciales_Fechas" CHECK ("FechaFin" > "FechaInicio");

ALTER TABLE "ListasPrecios"
    ADD CONSTRAINT "CK_ListasPrecios_Prioridad" CHECK ("Prioridad" BETWEEN 1 AND 999),
    ADD CONSTRAINT "CK_ListasPrecios_Fechas" CHECK ("FechaFin" IS NULL OR "FechaFin" >= "FechaInicio");

ALTER TABLE "ItemsListaPrecios"
    ADD CONSTRAINT "CK_ItemsListaPrecios_PrecioNoNegativo" CHECK ("Precio" >= 0);

ALTER TABLE "Promociones"
    ADD CONSTRAINT "CK_Promociones_Tipo" CHECK ("Tipo" IN ('Producto', 'MontoMinimo')),
    ADD CONSTRAINT "CK_Promociones_Porcentaje" CHECK ("PorcentajeDescuento" BETWEEN 0 AND 100),
    ADD CONSTRAINT "CK_Promociones_Fechas" CHECK ("FechaFin" >= "FechaInicio"),
    ADD CONSTRAINT "CK_Promociones_CoherenciaTipo" CHECK (
        ("Tipo" = 'Producto' AND "ProductoId" IS NOT NULL AND "MontoMinimo" IS NULL)
        OR ("Tipo" = 'MontoMinimo' AND "ProductoId" IS NULL AND "MontoMinimo" >= 0));

ALTER TABLE "Vendedores"
    ADD CONSTRAINT "CK_Vendedores_PorcentajeComision" CHECK ("PorcentajeComision" BETWEEN 0 AND 20);

ALTER TABLE "LiquidacionesComisiones"
    ADD CONSTRAINT "CK_LiquidacionesComisiones_Periodo" CHECK ("Anio" BETWEEN 2000 AND 2100 AND "Mes" BETWEEN 1 AND 12),
    ADD CONSTRAINT "CK_LiquidacionesComisiones_Ordenes" CHECK ("Ordenes" >= 1),
    ADD CONSTRAINT "CK_LiquidacionesComisiones_Importes" CHECK ("Ventas" >= 0 AND "Comision" >= 0),
    ADD CONSTRAINT "CK_LiquidacionesComisiones_Porcentaje" CHECK ("Porcentaje" BETWEEN 0 AND 20),
    ADD CONSTRAINT "CK_LiquidacionesComisiones_Estado" CHECK ("Estado" IN ('Calculada', 'Pagada'));

ALTER TABLE "ReclamosPostventa"
    ADD CONSTRAINT "CK_ReclamosPostventa_Tipo" CHECK ("Tipo" IN ('Defecto', 'Retraso', 'Faltante', 'Facturacion', 'Otro')),
    ADD CONSTRAINT "CK_ReclamosPostventa_Prioridad" CHECK ("Prioridad" IN ('Alta', 'Media', 'Baja')),
    ADD CONSTRAINT "CK_ReclamosPostventa_Estado" CHECK ("Estado" IN ('Abierto', 'EnProceso', 'Resuelto', 'Cerrado', 'Rechazado'));

ALTER TABLE "DevolucionesComerciales"
    ADD CONSTRAINT "CK_DevolucionesComerciales_Estado" CHECK ("Estado" IN ('Solicitada', 'Aprobada', 'Recibida', 'Rechazada'));

ALTER TABLE "DetallesDevolucion"
    ADD CONSTRAINT "CK_DetallesDevolucion_CantidadPositiva" CHECK ("Cantidad" > 0);

-- 3. Compras.
ALTER TABLE "SolicitudesCompra"
    ADD CONSTRAINT "CK_SolicitudesCompra_Estado" CHECK ("Estado" IN ('Pendiente', 'Aprobada', 'Rechazada'));

ALTER TABLE "DetallesSolicitudCompra"
    ADD CONSTRAINT "CK_DetallesSolicitudCompra_CantidadPositiva" CHECK ("Cantidad" > 0),
    ADD CONSTRAINT "CK_DetallesSolicitudCompra_PrecioNoNegativo" CHECK ("PrecioEstimado" >= 0);

ALTER TABLE "OrdenesAbastecimiento"
    ADD CONSTRAINT "CK_OrdenesAbastecimiento_Estado" CHECK ("Estado" IN ('Emitida', 'Recibida', 'Anulada'));

ALTER TABLE "DetallesOrdenesAbastecimiento"
    ADD CONSTRAINT "CK_DetallesOrdenesAbastecimiento_CantidadPositiva" CHECK ("Cantidad" > 0),
    ADD CONSTRAINT "CK_DetallesOrdenesAbastecimiento_PrecioNoNegativo" CHECK ("PrecioUnitario" >= 0);

-- 4. Inventarios, manufactura y logística.
ALTER TABLE "Almacenes"
    ADD CONSTRAINT "CK_Almacenes_Tipo" CHECK ("Tipo" IN ('Central', 'Regional', 'Transito')),
    ADD CONSTRAINT "CK_Almacenes_CapacidadNoNegativa" CHECK ("CapacidadM3" >= 0);

ALTER TABLE "ZonasAlmacenaje"
    ADD CONSTRAINT "CK_ZonasAlmacenaje_Tipo" CHECK ("Tipo" IN ('Recepcion', 'Almacenamiento', 'Picking', 'Despacho', 'Cuarentena')),
    ADD CONSTRAINT "CK_ZonasAlmacenaje_CapacidadNoNegativa" CHECK ("CapacidadM3" >= 0);

ALTER TABLE "CentrosTrabajo"
    ADD CONSTRAINT "CK_CentrosTrabajo_Tipo" CHECK ("Tipo" IN ('Linea', 'Celda')),
    ADD CONSTRAINT "CK_CentrosTrabajo_CapacidadHorasDia" CHECK ("CapacidadHorasDia" > 0 AND "CapacidadHorasDia" <= 24),
    ADD CONSTRAINT "CK_CentrosTrabajo_CostoHoraNoNegativo" CHECK ("CostoHora" >= 0);

ALTER TABLE "VehiculosFlota"
    ADD CONSTRAINT "CK_VehiculosFlota_Tipo" CHECK ("Tipo" IN ('Camion', 'Furgon', 'Moto', 'Otro')),
    ADD CONSTRAINT "CK_VehiculosFlota_Anio" CHECK ("Anio" >= 1900),
    ADD CONSTRAINT "CK_VehiculosFlota_CapacidadPositiva" CHECK ("CapacidadKg" > 0);

-- 5. Capital humano, seguridad e impuestos.
ALTER TABLE "Empleados"
    ADD CONSTRAINT "CK_Empleados_TipoDocumento" CHECK ("TipoDocumento" IN ('DNI', 'CE', 'Pasaporte')),
    ADD CONSTRAINT "CK_Empleados_Estado" CHECK ("Estado" IN ('Activo', 'Cesado')),
    ADD CONSTRAINT "CK_Empleados_IngresoTrasNacimiento" CHECK ("FechaIngreso" > "FechaNacimiento"),
    ADD CONSTRAINT "CK_Empleados_CeseTrasIngreso" CHECK ("FechaCese" IS NULL OR "FechaCese" >= "FechaIngreso");

ALTER TABLE "PermisosRoles"
    ADD CONSTRAINT "CK_PermisosRoles_Modulo" CHECK ("ModuloNumero" BETWEEN 1 AND 80),
    ADD CONSTRAINT "CK_PermisosRoles_EscrituraConLectura" CHECK (NOT "PuedeEscribir" OR "PuedeLeer");

ALTER TABLE "Impuestos"
    ADD CONSTRAINT "CK_Impuestos_Tipo" CHECK ("Tipo" IN ('IGV', 'IVA', 'RETENCION', 'PERCEPCION', 'DETRACCION'));

ALTER TABLE "TasasImpuestos"
    ADD CONSTRAINT "CK_TasasImpuestos_Porcentaje" CHECK ("Porcentaje" BETWEEN 0 AND 100),
    ADD CONSTRAINT "CK_TasasImpuestos_Fechas" CHECK ("FechaFin" IS NULL OR "FechaFin" >= "FechaInicio");

ALTER TABLE "RegistrosAuditoria"
    ADD CONSTRAINT "CK_RegistrosAuditoria_Accion" CHECK ("Accion" IN ('Alta', 'Modificacion', 'Baja'));
