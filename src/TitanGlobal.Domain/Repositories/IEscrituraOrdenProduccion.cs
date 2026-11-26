namespace TitanGlobal.Domain.Repositories;

public interface IEscrituraOrdenProduccion
{
    Task<int> EjecutarAsync(Func<Task<int>> operacion);

    Task<int> EjecutarBloqueandoAsync(int id, Func<Task<int>> operacion);
}
