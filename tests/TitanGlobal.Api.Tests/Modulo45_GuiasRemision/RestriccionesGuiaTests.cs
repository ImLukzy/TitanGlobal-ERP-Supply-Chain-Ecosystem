using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo45_GuiasRemision;

[Collection("Modulo04")]
public class RestriccionesGuiaTests
{
    private readonly OrdenesCompraApiFactory _fabrica;

    public RestriccionesGuiaTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        fabrica.SeedDatabase();
    }

    [Theory]
    [InlineData("crear", TipoViolacionRestriccion.Unicidad, "Ya existe una guía con la serie T001 y el número 1.")]
    [InlineData("editar", TipoViolacionRestriccion.Unicidad, "Ya existe una guía con la serie T001 y el número 1.")]
    [InlineData("eliminar", TipoViolacionRestriccion.ReferenciaForanea, "No se puede eliminar la guía porque está en uso.")]
    public async Task TraduceRestriccionesAlGuardar(string accion, TipoViolacionRestriccion tipo, string mensaje)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<LineaGuiaRemision>());
        db.RemoveRange(db.Set<GuiaRemision>());
        db.SaveChanges();
        db.Add(new GuiaRemision { Id = 1, Serie = "ORIGINAL", Numero = 1, Estado = "Borrador" });
        db.SaveChanges();
        IUnitOfWork unidad = new FallaAlGuardar(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), tipo);
        ReglaNegocioException error = await Assert.ThrowsAsync<ReglaNegocioException>(async () =>
        {
            if (accion == "crear")
            {
                await new CrearGuiaUseCase(unidad).ExecuteAsync(GuiasRemisionApiTests.Valida());
            }
            else if (accion == "editar")
            {
                await new ActualizarGuiaUseCase(unidad).ExecuteAsync(1, GuiasRemisionApiTests.Valida());
            }
            else
            {
                await new EliminarGuiaUseCase(unidad).ExecuteAsync(1);
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
