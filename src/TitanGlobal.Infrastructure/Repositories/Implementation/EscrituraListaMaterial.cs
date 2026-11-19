using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Infrastructure.Repositories.Implementation;

public class EscrituraListaMaterial : IEscrituraListaMaterial
{
    private readonly TitanGlobalDbContext _contexto;

    public EscrituraListaMaterial(TitanGlobalDbContext contexto)
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

        // La operación lee el estado después de obtener el bloqueo de la cabecera.
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"ListasMateriales\" WHERE \"Id\" = {id} FOR UPDATE");

        int resultado = await operacion();
        await transaccion.CommitAsync();
        return resultado;
    }
}
