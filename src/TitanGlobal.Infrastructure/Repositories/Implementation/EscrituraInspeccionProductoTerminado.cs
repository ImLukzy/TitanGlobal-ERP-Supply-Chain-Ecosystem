using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Infrastructure.Repositories.Implementation;

public class EscrituraInspeccionProductoTerminado : IEscrituraInspeccionProductoTerminado
{
    private readonly TitanGlobalDbContext _contexto;

    public EscrituraInspeccionProductoTerminado(TitanGlobalDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<int> EjecutarBloqueandoOrdenAsync(
        int ordenProduccionId, int? inspeccionId, Func<Task<int>> operacion)
    {
        if (!_contexto.Database.IsRelational())
        {
            return await operacion();
        }

        await using IDbContextTransaction transaccion =
            await _contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        // Primero la orden y después la inspección, siempre en el mismo orden, para no provocar interbloqueos.
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"OrdenesProduccion\" WHERE \"Id\" = {ordenProduccionId} FOR UPDATE");
        if (inspeccionId.HasValue)
        {
            await BloquearInspeccionAsync(inspeccionId.Value);
        }

        int resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }

    public async Task<int> EjecutarBloqueandoInspeccionAsync(int inspeccionId, Func<Task<int>> operacion)
    {
        if (!_contexto.Database.IsRelational())
        {
            return await operacion();
        }

        await using IDbContextTransaction transaccion =
            await _contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await BloquearInspeccionAsync(inspeccionId);
        int resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }

    private async Task BloquearInspeccionAsync(int inspeccionId)
    {
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"InspeccionesProductoTerminado\" WHERE \"Id\" = {inspeccionId} FOR UPDATE");
    }
}
