using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Infrastructure.Repositories.Implementation;

public class UnitOfWorkTesoreria : UnitOfWork, IUnitOfWorkTesoreria
{
    private readonly TitanGlobalDbContext _contexto;
    private readonly IConsultaConciliacionBancaria _conciliacion;

    public UnitOfWorkTesoreria(TitanGlobalDbContext contexto, IConsultaConciliacionBancaria conciliacion)
        : base(contexto)
    {
        _contexto = contexto;
        _conciliacion = conciliacion;
    }

    public async Task<TResult> ConCuentasBloqueadasAsync<TResult>(IReadOnlyCollection<int> cuentasId, Func<Task<TResult>> operacion)
    {
        if (!_contexto.Database.IsRelational())
        {
            return await operacion();
        }

        int[] ids = cuentasId.Distinct().ToArray();
        await using IDbContextTransaction transaccion = await _contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        // El ORDER BY del SELECT impone el mismo orden de bloqueo en ambos sentidos de una transferencia.
        await _contexto.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT "Id" FROM "CuentasBancarias" WHERE "Id" = ANY({ids}) ORDER BY "Id" FOR UPDATE
            """);
        TResult resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }

    public Task<bool> TieneCoincidenciaVigenteAsync(int movimientoId)
    {
        return _conciliacion.TieneCoincidenciaVigenteAsync(movimientoId);
    }
}
