namespace TitanGlobal.Domain.Repositories;

public interface IEscrituraInspeccionProductoTerminado
{
    Task<int> EjecutarBloqueandoOrdenAsync(int ordenProduccionId, int? inspeccionId, Func<Task<int>> operacion);

    Task<int> EjecutarBloqueandoInspeccionAsync(int inspeccionId, Func<Task<int>> operacion);
}
