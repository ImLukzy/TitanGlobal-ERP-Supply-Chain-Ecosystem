using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TitanGlobal.Api.Configuration.Scaffolding;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Arquitectura;

public class PluralizadorEspanolTests
{
    private readonly PluralizadorEspanol _pluralizador = new PluralizadorEspanol();

    [Fact]
    public void LasTablasDelModeloSeTraducenASuEntidadYVuelven()
    {
        using TitanGlobalDbContext contexto = new TitanGlobalDbContext(
            // Npgsql solo para leer los nombres de tabla del modelo; no abre ninguna conexión.
            new DbContextOptionsBuilder<TitanGlobalDbContext>().UseNpgsql("Host=localhost").Options);

        foreach (IEntityType entidad in contexto.Model.GetEntityTypes())
        {
            string tabla = entidad.GetTableName()!;
            Assert.Equal(entidad.ClrType.Name, _pluralizador.Singularize(tabla));
            Assert.Equal(tabla, _pluralizador.Pluralize(entidad.ClrType.Name));
            Assert.Equal(entidad.ClrType.Name, _pluralizador.Singularize(entidad.ClrType.Name));
        }
    }

    [Theory]
    [InlineData("MovimientosInventario", "MovimientoInventario")]
    [InlineData("UnidadesMedida", "UnidadMedida")]
    [InlineData("PerfilesUsuario", "PerfilUsuario")]
    [InlineData("PlanesProduccion", "PlanProduccion")]
    [InlineData("TalleresMantenimiento", "TallerMantenimiento")]
    [InlineData("ViajesFlota", "ViajeFlota")]
    [InlineData("LotesSeries", "LoteSeries")]
    [InlineData("DetallesGuia", "DetalleGuia")]
    [InlineData("LucesAlmacen", "LuzAlmacen")]
    [InlineData("SolicitudesTraslado", "SolicitudTraslado")]
    [InlineData("CierresPeriodo", "CierrePeriodo")]
    [InlineData("SedesEmpresa", "SedeEmpresa")]
    [InlineData("SucursalesEmpresa", "SucursalEmpresa")]
    [InlineData("TimbresFiscales", "TimbreFiscales")]
    public void UnaTablaNuevaSingularizaSoloLaPrimeraPalabra(string tabla, string entidad)
    {
        Assert.Equal(entidad, _pluralizador.Singularize(tabla));
    }
}
