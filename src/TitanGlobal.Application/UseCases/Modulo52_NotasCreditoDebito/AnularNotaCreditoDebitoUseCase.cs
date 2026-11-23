using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

public sealed class AnularNotaCreditoDebitoUseCase
{
    private readonly IUnitOfWorkFacturas _unitOfWork;

    public AnularNotaCreditoDebitoUseCase(IUnitOfWorkFacturas unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<NotaCreditoDebitoDetalleDto> ExecuteAsync(int id, AnularNotaCreditoDebitoRequestDto solicitud)
    {
        string motivo = ValidacionNotaCreditoDebito.NormalizarMotivoAnulacion(solicitud.Motivo);
        return PersistenciaNotaCreditoDebito.ModificarAsync(_unitOfWork, id, AnularEmitidaAsync);

        Task AnularEmitidaAsync(NotaCreditoDebito nota)
        {
            ValidacionNotaCreditoDebito.ValidarEstado(nota, NotaCreditoDebito.EstadoEmitida);
            nota.Estado = NotaCreditoDebito.EstadoAnulada;
            nota.FechaAnulacion = FechasUtc.Normalizar(DateTime.UtcNow);
            nota.MotivoAnulacion = motivo;
            return Task.CompletedTask;
        }
    }
}
