namespace TitanGlobal.Domain.Repositories;

public interface IConsultaConciliacionBancaria
{
    Task<bool> TieneCoincidenciaVigenteAsync(int movimientoId);
}
