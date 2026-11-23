using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

public sealed class EmitirNotaCreditoDebitoUseCase
{
    private readonly IUnitOfWorkFacturas _unitOfWork;

    public EmitirNotaCreditoDebitoUseCase(IUnitOfWorkFacturas unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<NotaCreditoDebitoDetalleDto> ExecuteAsync(int id)
    {
        return PersistenciaNotaCreditoDebito.ModificarAsync(_unitOfWork, id, EmitirBorradorAsync);
    }

    private async Task EmitirBorradorAsync(NotaCreditoDebito nota)
    {
        ValidacionNotaCreditoDebito.ValidarEstado(nota, NotaCreditoDebito.EstadoBorrador);
        await PersistenciaNotaCreditoDebito.ValidarFacturaEmitidaAsync(_unitOfWork, nota.FacturaId);
        nota.Estado = NotaCreditoDebito.EstadoEmitida;
        nota.FechaEmision = FechasUtc.Normalizar(DateTime.UtcNow);
    }
}
