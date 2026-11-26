namespace TitanGlobal.Domain.Repositories;

public interface IUnitOfWorkFacturas : IUnitOfWork
{
    Task<TResult> ConFacturaBloqueadaAsync<TResult>(int facturaId, Func<Task<TResult>> operacion);
    Task<long> SiguienteCorrelativoNotaAsync(string tipo);
}
