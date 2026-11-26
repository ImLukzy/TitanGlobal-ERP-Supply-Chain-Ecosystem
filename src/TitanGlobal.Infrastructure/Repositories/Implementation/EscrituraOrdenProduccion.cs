using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Infrastructure.Repositories.Implementation;

public class EscrituraOrdenProduccion : IEscrituraOrdenProduccion
{
    private readonly TitanGlobalDbContext _contexto;

    public EscrituraOrdenProduccion(TitanGlobalDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<int> EjecutarAsync(Func<Task<int>> operacion)
    {
        if (!_contexto.Database.IsRelational())
        {
            return await operacion();
        }

        await using IDbContextTransaction transaccion =
            await _contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        int resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }

    public async Task<int> EjecutarBloqueandoAsync(int id, Func<Task<int>> operacion)
    {
        if (!_contexto.Database.IsRelational())
        {
            return await operacion();
        }

        await using IDbContextTransaction transaccion =
            await _contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        // La aplicación consulta el estado después de adquirir el bloqueo de la fila.
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"OrdenesProduccion\" WHERE \"Id\" = {id} FOR UPDATE");
        int resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }
}
