using System.Transactions;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.UseCases;

public class CrearSolicitudCompraUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearSolicitudCompraUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SolicitudCreadaDto> ExecuteAsync(GuardarSolicitudRequestDto solicitud)
    {
        GuardarSolicitudRequestDto datosValidados = await ValidacionSolicitudCompra.ValidarAsync(solicitud, _unitOfWork);
        string numeroTemporal = $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
        SolicitudCompra solicitudNueva = new SolicitudCompra
        {
            Numero = numeroTemporal,
            Fecha = FechasUtc.Normalizar(DateTime.UtcNow),
            Solicitante = datosValidados.Solicitante,
            ProveedorId = datosValidados.ProveedorId,
            Estado = "Pendiente",
            Observaciones = datosValidados.Observaciones
        };
        foreach (LineaSolicitudRequestDto linea in datosValidados.Lineas!)
        {
            solicitudNueva.Detalles.Add(new DetalleSolicitudCompra
            {
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioEstimado = linea.PrecioEstimado
            });
        }

        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await _unitOfWork.Repository<SolicitudCompra>().AddAsync(solicitudNueva);
            await _unitOfWork.CompleteAsync();
            solicitudNueva.Numero = CrearNumero(solicitudNueva.Id);
            _unitOfWork.Repository<SolicitudCompra>().Update(solicitudNueva);
            await _unitOfWork.CompleteAsync();
            transaccion.Complete();
        }

        return new SolicitudCreadaDto
        {
            Id = solicitudNueva.Id,
            Numero = solicitudNueva.Numero
        };
    }

    private static string CrearNumero(int id)
    {
        return $"SC-{id:D6}";
    }
}
