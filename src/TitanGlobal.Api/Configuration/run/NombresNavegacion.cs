using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;

namespace TitanGlobal.Api.Configuration.Scaffolding;

#pragma warning disable EF1001 // Servicio interno de EF Core: solo se usa en tiempo de diseño para el scaffold.
// Conserva los nombres cortos de las colecciones que usa el código (Detalles, Zonas, Tasas…) en vez de los
// que el scaffold deduce de la tabla (DetallesCotizacion, ZonasAlmacenaje…).
public class NombresNavegacion : CandidateNamingService
{
    // Clave: «Principal.Dependiente». Una relación nueva solo se añade aquí si necesita un nombre propio.
    private static readonly Dictionary<string, string> Colecciones = new Dictionary<string, string>
    {
        { "Almacen.ZonaAlmacenaje", "Zonas" },
        { "Cotizacion.DetalleCotizacion", "Detalles" },
        { "DevolucionComercial.DetalleDevolucion", "Detalles" },
        { "Impuesto.TasaImpuesto", "Tasas" },
        { "ListaPrecios.ItemListaPrecios", "Items" },
        { "OrdenAbastecimiento.DetalleOrdenAbastecimiento", "Detalles" },
        { "OrdenCompra.DetalleOrdenCompra", "Detalles" },
        { "SolicitudCompra.DetalleSolicitudCompra", "Detalles" },
        { "Usuario.UsuarioRol", "Roles" },
        { "Rol.UsuarioRol", "Usuarios" }
    };

    public override string GetPrincipalEndCandidateNavigationPropertyName(
        IReadOnlyForeignKey foreignKey,
        string dependentEndNavigationPropertyName)
    {
        string clave = NombreCorto(foreignKey.PrincipalEntityType.Name) + "." + NombreCorto(foreignKey.DeclaringEntityType.Name);
        if (Colecciones.TryGetValue(clave, out string? nombre))
        {
            return nombre;
        }

        return base.GetPrincipalEndCandidateNavigationPropertyName(foreignKey, dependentEndNavigationPropertyName);
    }

    private static string NombreCorto(string nombre)
    {
        int punto = nombre.LastIndexOf('.');
        return punto < 0 ? nombre : nombre.Substring(punto + 1);
    }
}
#pragma warning restore EF1001
