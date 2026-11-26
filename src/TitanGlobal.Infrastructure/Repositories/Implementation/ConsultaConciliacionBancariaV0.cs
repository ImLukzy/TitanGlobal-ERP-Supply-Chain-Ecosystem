using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Infrastructure.Repositories.Implementation;

public class ConsultaConciliacionBancariaV0 : IConsultaConciliacionBancaria
{
    public Task<bool> TieneCoincidenciaVigenteAsync(int movimientoId)
    {
        // El módulo 56 registra la consulta a coincidencias con el DbContext de la misma transacción.
        return Task.FromResult(false);
    }
}
