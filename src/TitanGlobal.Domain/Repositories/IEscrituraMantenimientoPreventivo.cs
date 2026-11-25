namespace TitanGlobal.Domain.Repositories;

public interface IEscrituraMantenimientoPreventivo
{
    Task<int> EjecutarAsync(int id, Func<Task<int>> operacion);
}
