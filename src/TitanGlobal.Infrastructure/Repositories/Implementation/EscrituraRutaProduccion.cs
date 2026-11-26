using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Infrastructure.Repositories.Implementation;

public class EscrituraRutaProduccion : IEscrituraRutaProduccion
{
    private readonly TitanGlobalDbContext _contexto;

    public EscrituraRutaProduccion(TitanGlobalDbContext contexto)
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

        // La aplicación consulta el estado después de adquirir el bloqueo.
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"RutasProduccion\" WHERE \"Id\" = {id} FOR UPDATE");
        int resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }
}
