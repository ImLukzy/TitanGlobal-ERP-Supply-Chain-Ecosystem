using System.Globalization;
using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

public sealed class CrearNotaCreditoDebitoUseCase
{
    private readonly IUnitOfWorkFacturas _unitOfWork;

    public CrearNotaCreditoDebitoUseCase(IUnitOfWorkFacturas unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<NotaCreditoDebitoDetalleDto> ExecuteAsync(CrearNotaCreditoDebitoRequestDto solicitud)
    {
        ValidacionNotaCreditoDebito.ValidarFacturaId(solicitud.FacturaId);
        string tipo = ValidacionNotaCreditoDebito.ValidarTipo(solicitud.Tipo);
        string motivo = ValidacionNotaCreditoDebito.NormalizarMotivo(solicitud.Motivo);
        List<DetalleNotaCreditoDebitoRequestDto> entradas = ValidacionNotaCreditoDebito.ValidarDetalles(solicitud.Detalles);
        return _unitOfWork.ConFacturaBloqueadaAsync(solicitud.FacturaId, CrearBajoBloqueoAsync);

        async Task<NotaCreditoDebitoDetalleDto> CrearBajoBloqueoAsync()
        {
            Factura factura = await PersistenciaNotaCreditoDebito.ObtenerFacturaAsync(_unitOfWork, solicitud.FacturaId);
            List<DetalleNotaCreditoDebito> detalles = await CalculoNotaCreditoDebito.CalcularAsync(_unitOfWork, factura.Id, entradas);
            NotaCreditoDebito nota = new NotaCreditoDebito
            {
                FacturaId = factura.Id,
                Tipo = tipo,
                Numero = $"TMP-{Guid.NewGuid():N}"[..20],
                Moneda = factura.Moneda,
                Motivo = motivo,
                Estado = NotaCreditoDebito.EstadoBorrador,
                FechaCreacion = FechasUtc.Normalizar(DateTime.UtcNow),
                DetallesNotaCreditoDebito = detalles
            };
            CalculoNotaCreditoDebito.AsignarTotales(nota, detalles);
            PersistenciaNotaCreditoDebito.ValidarFacturaEmitida(factura);

            await _unitOfWork.Repository<NotaCreditoDebito>().AddAsync(nota);
            await PersistenciaNotaCreditoDebito.GuardarAsync(_unitOfWork);
            long correlativo = await _unitOfWork.SiguienteCorrelativoNotaAsync(tipo);
            string prefijo = tipo == NotaCreditoDebito.TipoCredito ? "NC" : "ND";
            nota.Numero = $"{prefijo}-{correlativo.ToString("D6", CultureInfo.InvariantCulture)}";
            await PersistenciaNotaCreditoDebito.GuardarAsync(_unitOfWork);
            return await PersistenciaNotaCreditoDebito.ObtenerDetalleAsync(_unitOfWork, nota.Id);
        }
    }
}
