using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo48_TransportistasTerceros;

[Collection("Modulo04")]
public class RestriccionesTransportistasTests
{
    private readonly OrdenesCompraApiFactory _fabrica;

    public RestriccionesTransportistasTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        fabrica.SeedDatabase();
    }

    [Theory]
    [InlineData("crear", TipoViolacionRestriccion.Unicidad, "Ya existe un transportista con el documento fiscal DOC-RACE en Perú.")]
    [InlineData("actualizar", TipoViolacionRestriccion.Unicidad, "Ya existe un transportista con el documento fiscal DOC-RACE en Perú.")]
    [InlineData("eliminar", TipoViolacionRestriccion.ReferenciaForanea, "No se puede eliminar un transportista con fletes registrados.")]
    [InlineData("flete", TipoViolacionRestriccion.Unicidad, "Ya existe un flete con la referencia RACE para este transportista.")]
    [InlineData("flete", TipoViolacionRestriccion.ReferenciaForanea, "El transportista indicado ya no está disponible.")]
    public async Task ConflictoAlGuardarConservaContratoSinFiltrarDetallesSql(string operacion, TipoViolacionRestriccion tipo, string mensaje)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.FletesTerceros.RemoveRange(db.FletesTerceros);
        db.TransportistasTerceros.RemoveRange(db.TransportistasTerceros);
        db.SaveChanges();
        db.TransportistasTerceros.Add(new TransportistaTercero { Id = 1, RazonSocial = "Original", DocumentoFiscal = "ORIGINAL", Pais = "Perú", TipoServicio = "Flete", Activo = true });
        db.SaveChanges();
        IUnitOfWork real = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        FallaAlGuardar unidad = new FallaAlGuardar(real, tipo);
        GuardarTransportistaTerceroRequestDto maestro = new GuardarTransportistaTerceroRequestDto { RazonSocial = "Nuevo", DocumentoFiscal = "DOC-RACE", Pais = "Perú", TipoServicio = "Ambos" };
        ReglaNegocioException error = await Assert.ThrowsAsync<ReglaNegocioException>(async () =>
        {
            if (operacion == "crear") { await new CrearTransportistaTerceroUseCase(unidad).ExecuteAsync(maestro); }
            else if (operacion == "actualizar") { await new ActualizarTransportistaTerceroUseCase(unidad).ExecuteAsync(1, maestro); }
            else if (operacion == "eliminar") { await new EliminarTransportistaTerceroUseCase(unidad).ExecuteAsync(1); }
            else { await new RegistrarFleteTerceroUseCase(unidad).ExecuteAsync(new RegistrarFleteTerceroRequestDto { TransportistaId = 1, Referencia = "RACE", Fecha = DateTime.UtcNow, Origen = "Lima", Destino = "Callao", Importe = 1, Moneda = "PEN" }); }
        });
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(mensaje, error.Message);
        Assert.Equal(1, unidad.Intentos);
    }

    private sealed class FallaAlGuardar : IUnitOfWork
    {
        private readonly IUnitOfWork _real;
        private readonly TipoViolacionRestriccion _tipo;

        public FallaAlGuardar(IUnitOfWork real, TipoViolacionRestriccion tipo)
        { _real = real; _tipo = tipo; }

        public int Intentos { get; private set; }

        public IGenericRepository<T> Repository<T>() where T : class
        { return _real.Repository<T>(); }

        public Task<int> CompleteAsync()
        {
            Intentos++;
            throw new ViolacionRestriccionException(_tipo, "restriccion", "Detalle interno", new InvalidOperationException("Detalle SQL"));
        }

        public void Dispose() { }
    }
}
