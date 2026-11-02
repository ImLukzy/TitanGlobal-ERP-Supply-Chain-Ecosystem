using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones;

public class CrearCotizacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearCotizacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CotizacionCreadaDto> ExecuteAsync(GuardarCotizacionRequestDto solicitud)
    {
        CotizacionNormalizada datos = await ValidacionCotizacion.ValidarAsync(solicitud, _unitOfWork);
        Cotizacion cotizacion = new Cotizacion
        {
            Numero = $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}",
            Fecha = FechasUtc.Normalizar(DateTime.UtcNow),
            ClienteId = solicitud.ClienteId,
            FechaValidez = datos.FechaValidez,
            Estado = "Emitida",
            Observaciones = datos.Observaciones
        };
        foreach (LineaCotizacionRequestDto linea in datos.Lineas)
        {
            cotizacion.Detalles.Add(new DetalleCotizacion
            {
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                DescuentoPorcentaje = linea.DescuentoPorcentaje
            });
        }

        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await _unitOfWork.Repository<Cotizacion>().AddAsync(cotizacion);
            await _unitOfWork.CompleteAsync();
            cotizacion.Numero = CrearNumero(cotizacion.Id);
            _unitOfWork.Repository<Cotizacion>().Update(cotizacion);
            await _unitOfWork.CompleteAsync();
            transaccion.Complete();
        }

        return new CotizacionCreadaDto
        {
            Id = cotizacion.Id,
            Numero = cotizacion.Numero
        };
    }

    private static string CrearNumero(int id)
    {
        return $"COT-{id:D6}";
    }
}
