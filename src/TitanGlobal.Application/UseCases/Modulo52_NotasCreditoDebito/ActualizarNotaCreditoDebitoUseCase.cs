using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

public sealed class ActualizarNotaCreditoDebitoUseCase
{
    private readonly IUnitOfWorkFacturas _unitOfWork;

    public ActualizarNotaCreditoDebitoUseCase(IUnitOfWorkFacturas unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<NotaCreditoDebitoDetalleDto> ExecuteAsync(int id, ActualizarNotaCreditoDebitoRequestDto solicitud)
    {
        string motivo = ValidacionNotaCreditoDebito.NormalizarMotivo(solicitud.Motivo);
        List<DetalleNotaCreditoDebitoRequestDto> entradas = ValidacionNotaCreditoDebito.ValidarDetalles(solicitud.Detalles);
        return PersistenciaNotaCreditoDebito.ModificarAsync(_unitOfWork, id, ActualizarBorradorAsync);

        async Task ActualizarBorradorAsync(NotaCreditoDebito nota)
        {
            List<DetalleNotaCreditoDebito> detalles = await CalculoNotaCreditoDebito.CalcularAsync(
                _unitOfWork, nota.FacturaId, entradas);
            CalculoNotaCreditoDebito.AsignarTotales(nota, detalles);
            ValidacionNotaCreditoDebito.ValidarEstado(nota, NotaCreditoDebito.EstadoBorrador);

            nota.Motivo = motivo;
            await PersistenciaNotaCreditoDebito.ActualizarDetallesAsync(_unitOfWork, nota, detalles);
        }
    }
}
