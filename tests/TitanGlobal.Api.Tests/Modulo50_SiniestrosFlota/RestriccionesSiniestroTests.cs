using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo50_SiniestrosFlota;

[Collection("Modulo04")]
public class RestriccionesSiniestroTests
{
    private readonly OrdenesCompraApiFactory _fabrica;

    public RestriccionesSiniestroTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        fabrica.SeedDatabase();
    }

    [Theory]
    [InlineData("crear", TipoViolacionRestriccion.Unicidad, "Ya existe un siniestro con el código SIN-50.")]
    [InlineData("editar", TipoViolacionRestriccion.Unicidad, "Ya existe un siniestro con el código SIN-50.")]
    [InlineData("eliminar", TipoViolacionRestriccion.ReferenciaForanea, "No se puede eliminar el siniestro porque está en uso.")]
    public async Task TraduceRestriccionesAlGuardar(string accion, TipoViolacionRestriccion tipo, string mensaje)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<SiniestroFlota>());
        db.SaveChanges();
        db.Add(new SiniestroFlota { Id = 1, Codigo = "ORIGINAL", Estado = "Registrado" });
        db.SaveChanges();
        IUnitOfWork unidad = new FallaAlGuardar(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), tipo);
        ReglaNegocioException error = await Assert.ThrowsAsync<ReglaNegocioException>(async () =>
        {
            if (accion == "crear")
            {
                await new CrearSiniestroUseCase(unidad).ExecuteAsync(SiniestrosApiTests.Valida());
            }
            else if (accion == "editar")
            {
                await new ActualizarSiniestroUseCase(unidad).ExecuteAsync(1, SiniestrosApiTests.Valida());
            }
            else
            {
                await new EliminarSiniestroUseCase(unidad).ExecuteAsync(1);
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
