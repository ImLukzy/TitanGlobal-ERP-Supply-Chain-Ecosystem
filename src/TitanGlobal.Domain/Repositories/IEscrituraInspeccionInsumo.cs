namespace TitanGlobal.Domain.Repositories;

public interface IEscrituraInspeccionInsumo
{
    Task<int> EjecutarAsync(int id, Func<Task<int>> operacion);
}
