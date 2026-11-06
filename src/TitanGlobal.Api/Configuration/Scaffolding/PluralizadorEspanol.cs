using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Design;

namespace TitanGlobal.Api.Configuration.Scaffolding;

// Solo se usa en tiempo de diseño, con dotnet ef dbcontext scaffold: traduce los nombres de tabla en
// plural (Clientes, OrdenesCompra) a nombres de entidad en singular (Cliente, OrdenCompra) y al revés.
public class PluralizadorEspanol : IPluralizer
{
    // Tabla → entidad. Una tabla nueva cuyo nombre no siga la regla general se añade aquí.
    private static readonly Dictionary<string, string> EntidadPorTabla = new Dictionary<string, string>
    {
        { "AlertasDescartadas", "AlertaDescartada" },
        { "Almacenes", "Almacen" },
        { "AsignacionesVentas", "AsignacionVenta" },
        { "CategoriasProducto", "CategoriaProducto" },
        { "CentrosTrabajo", "CentroTrabajo" },
        { "Clientes", "Cliente" },
        { "Conductores", "Conductor" },
        { "ContratosComerciales", "ContratoComercial" },
        { "Cotizaciones", "Cotizacion" },
        { "DetallesCotizacion", "DetalleCotizacion" },
        { "DetallesDevolucion", "DetalleDevolucion" },
        { "DetallesOrdenesAbastecimiento", "DetalleOrdenAbastecimiento" },
        { "DetallesOrdenCompra", "DetalleOrdenCompra" },
        { "DetallesSolicitudCompra", "DetalleSolicitudCompra" },
        { "DevolucionesComerciales", "DevolucionComercial" },
        { "Empleados", "Empleado" },
        { "Impuestos", "Impuesto" },
        { "ItemsListaPrecios", "ItemListaPrecios" },
        { "LiquidacionesComisiones", "LiquidacionComision" },
        { "ListasPrecios", "ListaPrecios" },
        { "OrdenesAbastecimiento", "OrdenAbastecimiento" },
        { "OrdenesCompra", "OrdenCompra" },
        { "PermisosRoles", "PermisoRol" },
        { "Productos", "Producto" },
        { "Promociones", "Promocion" },
        { "Proveedores", "Proveedor" },
        { "ReclamosPostventa", "ReclamoPostventa" },
        { "RegistrosAuditoria", "RegistroAuditoria" },
        { "Roles", "Rol" },
        { "SegmentosClientes", "SegmentoCliente" },
        { "SolicitudesCompra", "SolicitudCompra" },
        { "TasasImpuestos", "TasaImpuesto" },
        { "Usuarios", "Usuario" },
        { "UsuariosRoles", "UsuarioRol" },
        { "VehiculosFlota", "VehiculoFlota" },
        { "Vendedores", "Vendedor" },
        { "ZonasAlmacenaje", "ZonaAlmacenaje" },
    };

    private static readonly Dictionary<string, string> TablaPorEntidad = EntidadPorTabla
        .ToDictionary(par => par.Value, par => par.Key);

    private static readonly Regex PrimeraPalabra = new Regex("^[A-Z][a-z]+", RegexOptions.CultureInvariant);

    public string Pluralize(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return identifier;
        }

        if (TablaPorEntidad.TryGetValue(identifier, out string? tabla))
        {
            return tabla;
        }

        if (EntidadPorTabla.ContainsKey(identifier))
        {
            return identifier;
        }

        return CambiarPrimeraPalabra(identifier, true);
    }

    public string Singularize(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return identifier;
        }

        if (EntidadPorTabla.TryGetValue(identifier, out string? entidad))
        {
            return entidad;
        }

        if (TablaPorEntidad.ContainsKey(identifier))
        {
            return identifier;
        }

        return CambiarPrimeraPalabra(identifier, false);
    }

    // Regla general: solo cambia la primera palabra (OrdenesCompra ↔ OrdenCompra, MovimientosInventario ↔ MovimientoInventario).
    private static string CambiarPrimeraPalabra(string identificador, bool plural)
    {
        Match palabra = PrimeraPalabra.Match(identificador);
        if (!palabra.Success)
        {
            return identificador;
        }

        string resto = identificador.Substring(palabra.Length);
        string nueva = plural ? APlural(palabra.Value) : ASingular(palabra.Value);
        return nueva + resto;
    }

    private static string APlural(string palabra)
    {
        if (palabra.EndsWith('s'))
        {
            return palabra;
        }

        return "aeiou".Contains(palabra[^1]) ? palabra + "s" : palabra + "es";
    }

    private static string ASingular(string palabra)
    {
        if (palabra.EndsWith("ones", StringComparison.Ordinal) || palabra.EndsWith("ores", StringComparison.Ordinal)
            || palabra.EndsWith("enes", StringComparison.Ordinal) || palabra.EndsWith("ales", StringComparison.Ordinal))
        {
            return palabra.Substring(0, palabra.Length - 2);
        }

        return palabra.EndsWith('s') ? palabra.Substring(0, palabra.Length - 1) : palabra;
    }
}
