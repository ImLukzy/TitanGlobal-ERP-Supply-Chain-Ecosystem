using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo46_MonitoreoEntregas;

[Collection("Modulo04")]
public class RestriccionesEntregaTests
{
    private readonly OrdenesCompraApiFactory _fabrica;

    public RestriccionesEntregaTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        fabrica.SeedDatabase();
    }

    [Theory]
    [InlineData("crear", TipoViolacionRestriccion.Unicidad, "Ya existe una entrega con el código ENV-46.")]
    [InlineData("editar", TipoViolacionRestriccion.Unicidad, "Ya existe una entrega con el código ENV-46.")]
    [InlineData("eliminar", TipoViolacionRestriccion.ReferenciaForanea, "No se puede eliminar la entrega porque está en uso.")]
    public async Task TraduceRestriccionesAlGuardar(string accion, TipoViolacionRestriccion tipo, string mensaje)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<EventoEntrega>());
        db.RemoveRange(db.Set<EntregaDespacho>());
        db.SaveChanges();
        db.Add(new EntregaDespacho { Id = 1, Codigo = "ORIGINAL", Estado = "Programada" });
        db.SaveChanges();
        IUnitOfWork unidad = new FallaAlGuardar(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), tipo);
        ReglaNegocioException error = await Assert.ThrowsAsync<ReglaNegocioException>(async () =>
        {
            if (accion == "crear")
            {
                await new CrearEntregaUseCase(unidad).ExecuteAsync(MonitoreoEntregasApiTests.Valida());
            }
            else if (accion == "editar")
            {
                await new ActualizarEntregaUseCase(unidad).ExecuteAsync(1, MonitoreoEntregasApiTests.Valida());
            }
            else
            {
                await new EliminarEntregaUseCase(unidad).ExecuteAsync(1);
            }
        });
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(mensaje, error.Message);
    }

    private sealed class FallaAlGuardar : IUnitOfWork
    {
        private readonly IUnitOfWork _real;
        private readonly TipoViolacionRestriccion _tipo;

        public FallaAlGuardar(IUnitOfWork real, TipoViolacionRestriccion tipo)
        {
            _real = real;
            _tipo = tipo;
        }

        public IGenericRepository<T> Repository<T>() where T : class
        {
            return _real.Repository<T>();
        }

        public Task<int> CompleteAsync()
        {
            throw new ViolacionRestriccionException(_tipo, "restriccion", "Interno", new InvalidOperationException("SQL interno"));
        }

        public void Dispose()
        {
        }
    }
}
