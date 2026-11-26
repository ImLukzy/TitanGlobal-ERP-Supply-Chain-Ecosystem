using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo44_ConsolidacionCarga;

[Collection("Modulo04")]
public class RestriccionesCargaTests
{
    private readonly OrdenesCompraApiFactory _fabrica;
    public RestriccionesCargaTests(OrdenesCompraApiFactory fabrica) { _fabrica = fabrica; fabrica.SeedDatabase(); }

    [Theory]
    [InlineData("crear", TipoViolacionRestriccion.Unicidad, "Ya existe una consolidación con el código NUEVA.")]
    [InlineData("editar", TipoViolacionRestriccion.Unicidad, "Ya existe una consolidación con el código NUEVA.")]
    [InlineData("eliminar", TipoViolacionRestriccion.ReferenciaForanea, "No se puede eliminar la consolidación porque está en uso.")]
    public async Task TraduceRestriccionesAlGuardarSinExponerSql(string accion, TipoViolacionRestriccion tipo, string mensaje)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope(); TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.LineasConsolidacionCarga.RemoveRange(db.LineasConsolidacionCarga); db.SaveChanges(); db.ConsolidacionesCarga.RemoveRange(db.ConsolidacionesCarga); db.SaveChanges();
        db.ConsolidacionesCarga.Add(new ConsolidacionCarga { Id = 1, Codigo = "ORIGINAL", Nombre = "Original", Estado = "Borrador", Lineas = new List<LineaConsolidacionCarga> { new LineaConsolidacionCarga { Posicion = 1, Sku = "S", Descripcion = "D", Cantidad = 1 } } }); db.SaveChanges();
        IUnitOfWork unidad = new FallaAlGuardar(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), tipo);
        ReglaNegocioException error = await Assert.ThrowsAsync<ReglaNegocioException>(async () =>
        {
            if (accion == "crear") { await new CrearConsolidacionUseCase(unidad).ExecuteAsync(ConsolidacionesApiTests.Solicitud()); }
            else if (accion == "editar") { await new ActualizarConsolidacionUseCase(unidad).ExecuteAsync(1, ConsolidacionesApiTests.Solicitud()); }
            else { await new EliminarConsolidacionUseCase(unidad).ExecuteAsync(1); }
        });
        Assert.Equal(409, error.StatusCode); Assert.Equal(mensaje, error.Message);
    }
    private sealed class FallaAlGuardar : IUnitOfWork
    {
        private readonly IUnitOfWork _real; private readonly TipoViolacionRestriccion _tipo;
        public FallaAlGuardar(IUnitOfWork real, TipoViolacionRestriccion tipo) { _real = real; _tipo = tipo; }
        public IGenericRepository<T> Repository<T>() where T : class { return _real.Repository<T>(); }
        public Task<int> CompleteAsync() { throw new ViolacionRestriccionException(_tipo, "restriccion", "Interno", new InvalidOperationException("SQL interno")); }
        public void Dispose() { }
    }
}
