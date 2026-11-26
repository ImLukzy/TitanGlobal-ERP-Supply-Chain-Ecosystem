namespace TitanGlobal.Domain.Repositories;

public interface IEscrituraRutaProduccion
{
    Task<int> EjecutarAsync(int id, Func<Task<int>> operacion);
}
