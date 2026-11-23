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

    public Task<NotaCreditoDebitoDetalleDto> ExecuteAsync(int id, GuardarNotaCreditoDebitoRequestDto solicitud)
    {
        DatosNotaCreditoDebito datos = ValidacionNotaCreditoDebito.ValidarSolicitud(solicitud);
        return PersistenciaNotaCreditoDebito.ModificarAsync(_unitOfWork, id, ActualizarBorradorAsync);

        async Task ActualizarBorradorAsync(NotaCreditoDebito nota)
        {
            ValidacionNotaCreditoDebito.ValidarFacturaId(datos.FacturaId, nota.FacturaId);
            ValidacionNotaCreditoDebito.ValidarEstado(nota, NotaCreditoDebito.EstadoBorrador);
            await PersistenciaNotaCreditoDebito.ValidarFacturaEmitidaAsync(_unitOfWork, nota.FacturaId);
            await PersistenciaNotaCreditoDebito.ValidarNumeracionAsync(_unitOfWork, datos, nota.Id);

            nota.Tipo = datos.Tipo;
            nota.Serie = datos.Serie;
            nota.Numero = datos.Numero;
            nota.Motivo = datos.Motivo;
            nota.Total = datos.Importe;
        }
    }
}
