namespace TitanGlobal.Domain.Repositories;

public interface IUnitOfWorkCuentasPorPagar : IUnitOfWork
{
    Task<TResult> ConCuentaBloqueadaAsync<TResult>(int cuentaId, Func<Task<TResult>> operacion);

    Task<TResult> ConReferenciasBloqueadasAsync<TResult>(int proveedorId, int? ordenId, Func<Task<TResult>> operacion);
}
