using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Infrastructure.Repositories.Implementation;

public class EscrituraMantenimientoPreventivo : IEscrituraMantenimientoPreventivo
{
    private readonly TitanGlobalDbContext _contexto;

    public EscrituraMantenimientoPreventivo(TitanGlobalDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<int> EjecutarAsync(int id, Func<Task<int>> operacion)
    {
        if (!_contexto.Database.IsRelational())
        {
            return await operacion();
        }

        await using IDbContextTransaction transaccion =
            await _contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        // La aplicación consulta el estado después de adquirir el bloqueo de la fila.
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"MantenimientosPreventivos\" WHERE \"Id\" = {id} FOR UPDATE");
        int resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }
}
