using System.Transactions;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.UseCases;

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
