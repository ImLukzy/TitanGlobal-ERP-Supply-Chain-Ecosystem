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

    public Task<NotaCreditoDebitoDetalleDto> ExecuteAsync(GuardarNotaCreditoDebitoRequestDto solicitud)
    {
        DatosNotaCreditoDebito datos = ValidacionNotaCreditoDebito.ValidarSolicitud(solicitud);
        return _unitOfWork.ConFacturaBloqueadaAsync(datos.FacturaId, CrearBajoBloqueoAsync);

        async Task<NotaCreditoDebitoDetalleDto> CrearBajoBloqueoAsync()
        {
            await PersistenciaNotaCreditoDebito.ValidarFacturaEmitidaAsync(_unitOfWork, datos.FacturaId);
            await PersistenciaNotaCreditoDebito.ValidarNumeracionAsync(_unitOfWork, datos);

            NotaCreditoDebito nota = new NotaCreditoDebito
            {
                FacturaId = datos.FacturaId,
                Tipo = datos.Tipo,
                Serie = datos.Serie,
                Numero = datos.Numero,
                Motivo = datos.Motivo,
                Total = datos.Importe,
                Estado = NotaCreditoDebito.EstadoBorrador,
                FechaCreacion = FechasUtc.Normalizar(DateTime.UtcNow)
            };

            await _unitOfWork.Repository<NotaCreditoDebito>().AddAsync(nota);
            await PersistenciaNotaCreditoDebito.GuardarAsync(_unitOfWork);
            return await PersistenciaNotaCreditoDebito.ObtenerDetalleAsync(_unitOfWork, nota.Id);
        }
    }
}
