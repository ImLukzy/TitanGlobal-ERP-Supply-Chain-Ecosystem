using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo43_RutasDespacho.Commands;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo43_RutasDespacho;

[Collection("Modulo04")]
public class RestriccionesRutasDespachoTests
{
    private readonly OrdenesCompraApiFactory _fabrica;

    public RestriccionesRutasDespachoTests(OrdenesCompraApiFactory fabrica) { _fabrica = fabrica; fabrica.SeedDatabase(); }

    [Theory]
    [InlineData("crear", TipoViolacionRestriccion.Unicidad, "Ya existe una ruta de despacho con el código NUEVA.")]
    [InlineData("editar", TipoViolacionRestriccion.Unicidad, "Ya existe una ruta de despacho con el código NUEVA.")]
    [InlineData("eliminar", TipoViolacionRestriccion.ReferenciaForanea, "No se puede eliminar la ruta de despacho porque está en uso.")]
    public async Task TraduceConflictosDuranteElGuardado(string operacion, TipoViolacionRestriccion tipo, string mensaje)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RutasDespacho.RemoveRange(db.RutasDespacho); db.SaveChanges();
        db.RutasDespacho.Add(new RutaDespacho { Id = 1, Codigo = "ORIGINAL", Nombre = "Original", Zona = "Centro", Origen = "Lima", Destino = "Callao", DistanciaKm = 1, PeajeEstimado = 0, Moneda = "PEN", Activo = true }); db.SaveChanges();
        IUnitOfWork unidad = new ConflictoAlGuardar(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), tipo);
        GuardarRutaDespachoRequestDto datos = RutasDespachoApiTests.Solicitud();
        ReglaNegocioException error = await Assert.ThrowsAsync<ReglaNegocioException>(async () =>
        {
            if (operacion == "crear") { await new CrearRutaDespachoCommandHandler(unidad).Handle(new CrearRutaDespachoCommand { Solicitud = datos }, CancellationToken.None); }
            else if (operacion == "editar") { await new ActualizarRutaDespachoCommandHandler(unidad).Handle(new ActualizarRutaDespachoCommand { Id = 1, Solicitud = datos }, CancellationToken.None); }
            else { await new EliminarRutaDespachoCommandHandler(unidad).Handle(new EliminarRutaDespachoCommand { Id = 1 }, CancellationToken.None); }
        });
        Assert.Equal(409, error.StatusCode); Assert.Equal(mensaje, error.Message);
    }

    private sealed class ConflictoAlGuardar : IUnitOfWork
    {
        private readonly IUnitOfWork _real;
        private readonly TipoViolacionRestriccion _tipo;
        public ConflictoAlGuardar(IUnitOfWork real, TipoViolacionRestriccion tipo) { _real = real; _tipo = tipo; }
        public IGenericRepository<T> Repository<T>() where T : class { return _real.Repository<T>(); }
        public Task<int> CompleteAsync() { throw new ViolacionRestriccionException(_tipo, "restriccion", "Información interna", new InvalidOperationException("SQL interno")); }
        public void Dispose() { }
    }
}
