using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo73_Sucursales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo73_Sucursales;

[Collection("Modulo04")]
public class RestriccionesSucursalTests
{
    private readonly OrdenesCompraApiFactory _fabrica;

    public RestriccionesSucursalTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        fabrica.SeedDatabase();
    }

    [Theory]
    [InlineData("crear", TipoViolacionRestriccion.Unicidad, "Ya existe una sucursal con el código SUC-73.")]
    [InlineData("editar", TipoViolacionRestriccion.Unicidad, "Ya existe una sucursal con el código SUC-73.")]
    [InlineData("eliminar", TipoViolacionRestriccion.ReferenciaForanea, "No se puede eliminar la sucursal porque está en uso.")]
    public async Task TraduceRestriccionesAlGuardar(string accion, TipoViolacionRestriccion tipo, string mensaje)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.RemoveRange(db.Set<Sucursal>());
        db.SaveChanges();
        db.Add(new Sucursal { Id = 1, Codigo = "ORIGINAL", Activa = false });
        db.SaveChanges();
        IUnitOfWork unidad = new FallaAlGuardar(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), tipo);
        ReglaNegocioException error = await Assert.ThrowsAsync<ReglaNegocioException>(async () =>
        {
            if (accion == "crear")
            {
                await new CrearSucursalUseCase(unidad).ExecuteAsync(SucursalesApiTests.Valida());
            }
            else if (accion == "editar")
            {
                await new ActualizarSucursalUseCase(unidad).ExecuteAsync(1, SucursalesApiTests.Valida());
            }
            else
            {
                await new EliminarSucursalUseCase(unidad).ExecuteAsync(1);
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
