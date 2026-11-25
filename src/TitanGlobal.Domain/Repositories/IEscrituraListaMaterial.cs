namespace TitanGlobal.Domain.Repositories;

public interface IEscrituraListaMaterial
{
    Task<int> EjecutarAsync(int id, Func<Task<int>> operacion);
}
