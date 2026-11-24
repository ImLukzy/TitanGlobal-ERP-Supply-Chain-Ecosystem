namespace TitanGlobal.Domain.Repositories;

public interface IUnitOfWorkCentrosCosto : IUnitOfWork
{
    Task<TResult> ConCentrosCostoBloqueadosAsync<TResult>(Func<Task<TResult>> operacion);
}
