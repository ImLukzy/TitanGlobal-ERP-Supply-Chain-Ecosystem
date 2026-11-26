namespace TitanGlobal.Domain.Repositories;

public interface IUnitOfWorkTesoreria : IUnitOfWork
{
    Task<TResult> ConCuentasBloqueadasAsync<TResult>(IReadOnlyCollection<int> cuentasId, Func<Task<TResult>> operacion);

    Task<bool> TieneCoincidenciaVigenteAsync(int movimientoId);
}
