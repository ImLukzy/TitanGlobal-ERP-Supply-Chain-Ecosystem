using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

public sealed class EmitirNotaCreditoDebitoUseCase
{
    private readonly IUnitOfWorkFacturas _unitOfWork;
    private readonly TimeProvider _reloj;

    public EmitirNotaCreditoDebitoUseCase(IUnitOfWorkFacturas unitOfWork, TimeProvider? reloj = null)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj ?? TimeProvider.System;
    }

    public Task<NotaCreditoDebitoDetalleDto> ExecuteAsync(int id)
    {
        return PersistenciaNotaCreditoDebito.ModificarAsync(_unitOfWork, id, EmitirBorradorAsync);
    }

    private async Task EmitirBorradorAsync(NotaCreditoDebito nota)
    {
        Factura factura = await PersistenciaNotaCreditoDebito.ObtenerFacturaAsync(_unitOfWork, nota.FacturaId);
        ValidacionNotaCreditoDebito.ValidarEstado(nota, NotaCreditoDebito.EstadoBorrador);
        PersistenciaNotaCreditoDebito.ValidarFacturaEmitida(factura);
        if (nota.Tipo == NotaCreditoDebito.TipoCredito)
        {
            await PersistenciaNotaCreditoDebito.ValidarTopesCreditoAsync(_unitOfWork, nota);
        }

        nota.FechaEmision = FechaNegocioPeru.DesdeUtc(_reloj.GetUtcNow().UtcDateTime);
        nota.Estado = NotaCreditoDebito.EstadoEmitida;
    }
}
