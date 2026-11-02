using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Infrastructure.Persistence;
using TitanGlobal.Infrastructure.Repositories.Implementation;

namespace TitanGlobal.Api.Tests.Arquitectura;

public class UnitOfWorkTests
{
    [Fact]
    public async Task CompleteAsync_TraduceViolacionDeUnicidad()
    {
        UnitOfWork unitOfWork = CrearConFallo(PostgresErrorCodes.UniqueViolation, "IX_Usuarios_Email");

        ViolacionRestriccionException excepcion =
            await Assert.ThrowsAsync<ViolacionRestriccionException>(() => unitOfWork.CompleteAsync());

        Assert.Equal(TipoViolacionRestriccion.Unicidad, excepcion.Tipo);
        Assert.Equal("IX_Usuarios_Email", excepcion.Restriccion);
        Assert.Equal("fallo al guardar", excepcion.Message);
        Assert.IsType<DbUpdateException>(excepcion.InnerException);
    }

    [Fact]
    public async Task CompleteAsync_TraduceViolacionDeClaveForanea()
    {
        UnitOfWork unitOfWork = CrearConFallo(PostgresErrorCodes.ForeignKeyViolation, "FK_Ordenes_Clientes");

        ViolacionRestriccionException excepcion =
            await Assert.ThrowsAsync<ViolacionRestriccionException>(() => unitOfWork.CompleteAsync());

        Assert.Equal(TipoViolacionRestriccion.ReferenciaForanea, excepcion.Tipo);
    }

    [Fact]
    public async Task CompleteAsync_NoTraduceOtrosErroresDePostgres()
    {
        UnitOfWork unitOfWork = CrearConFallo(PostgresErrorCodes.CheckViolation, "CK_Prueba");

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.CompleteAsync());
    }

    private static UnitOfWork CrearConFallo(string sqlState, string restriccion)
    {
        DbContextOptions<TitanGlobalDbContext> opciones = new DbContextOptionsBuilder<TitanGlobalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UnitOfWork(new ContextoConFallo(opciones, sqlState, restriccion));
    }

    private sealed class ContextoConFallo : TitanGlobalDbContext
    {
        private readonly string _sqlState;

        private readonly string _restriccion;

        public ContextoConFallo(DbContextOptions<TitanGlobalDbContext> opciones, string sqlState, string restriccion)
            : base(opciones)
        {
            _sqlState = sqlState;
            _restriccion = restriccion;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            PostgresException postgres = new PostgresException(
                "violación",
                "ERROR",
                "ERROR",
                _sqlState,
                constraintName: _restriccion);

            throw new DbUpdateException("fallo al guardar", postgres);
        }
    }
}
