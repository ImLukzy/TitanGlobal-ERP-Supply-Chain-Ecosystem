using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

public sealed class EmitirNotaCreditoDebitoUseCase
{
    private static readonly TimeZoneInfo ZonaLima = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
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
        Factura factura = await PersistenciaNotaCreditoDebito.ObtenerFacturaAsync(_unitOfWork, nota.FacturaId);
        ValidacionNotaCreditoDebito.ValidarEstado(nota, NotaCreditoDebito.EstadoBorrador);
        PersistenciaNotaCreditoDebito.ValidarFacturaEmitida(factura);
        if (nota.Tipo == NotaCreditoDebito.TipoCredito)
        {
            await PersistenciaNotaCreditoDebito.ValidarTopesCreditoAsync(_unitOfWork, nota);
        }

        nota.FechaEmision = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaLima));
        nota.Estado = NotaCreditoDebito.EstadoEmitida;
    }
}
