using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Infrastructure.Repositories.Implementation;

public class UnitOfWorkCuentasPorPagar : UnitOfWork, IUnitOfWorkCuentasPorPagar
{
    private readonly TitanGlobalDbContext _contexto;

    public UnitOfWorkCuentasPorPagar(TitanGlobalDbContext contexto)
        : base(contexto)
    {
        _contexto = contexto;
    }

    public Task<TResult> ConCuentaBloqueadaAsync<TResult>(int cuentaId, Func<Task<TResult>> operacion)
    {
        return ConBloqueosAsync(
            () => _contexto.Database.ExecuteSqlInterpolatedAsync($"""SELECT "Id" FROM "CuentasPorPagar" WHERE "Id" = {cuentaId} FOR UPDATE"""),
            operacion);
    }

    public Task<TResult> ConReferenciasBloqueadasAsync<TResult>(int proveedorId, int? ordenId, Func<Task<TResult>> operacion)
    {
        return ConBloqueosAsync(async () =>
        {
            // Mantener la vigencia del proveedor y su compatibilidad con la orden hasta confirmar el alta.
            await _contexto.Database.ExecuteSqlInterpolatedAsync($"""SELECT "Id" FROM "Proveedores" WHERE "Id" = {proveedorId} FOR SHARE""");
            if (ordenId.HasValue)
            {
                await _contexto.Database.ExecuteSqlInterpolatedAsync($"""SELECT "Id" FROM "OrdenesAbastecimiento" WHERE "Id" = {ordenId.Value} FOR SHARE""");
            }
        }, operacion);
    }

    private async Task<TResult> ConBloqueosAsync<TResult>(Func<Task> bloquear, Func<Task<TResult>> operacion)
    {
        if (!_contexto.Database.IsRelational())
        {
            return await operacion();
        }

        await using IDbContextTransaction transaccion = await _contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await bloquear();
        TResult resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }
}
