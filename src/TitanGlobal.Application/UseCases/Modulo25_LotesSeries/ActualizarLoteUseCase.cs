using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

public class ActualizarLoteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarLoteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, ActualizarLoteRequestDto solicitud)
    {
        Lote? lote = await _unitOfWork.Repository<Lote>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == id);
        if (lote is null)
        {
            throw ValidacionLoteSerie.CrearLoteNoEncontrado(id);
        }

        int cantidad = ValidacionLoteSerie.ValidarCantidadLote(solicitud.Cantidad);
        string estado = ValidacionLoteSerie.NormalizarEstadoLote(solicitud.Estado);
        DateTime? fabricacion = ValidacionLoteSerie.NormalizarFecha(solicitud.FechaFabricacion);
        DateTime? vencimiento = ValidacionLoteSerie.NormalizarFecha(solicitud.FechaVencimiento);
        ValidacionLoteSerie.ValidarFechas(fabricacion, vencimiento);
        int totalSeries = await _unitOfWork.Repository<UnidadSerie>()
            .AsQueryable()
            .CountAsync(registro => registro.LoteId == id);
        if (cantidad < totalSeries)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La cantidad del lote no puede ser menor que sus series registradas ({totalSeries}).");
        }

        lote.FechaFabricacion = fabricacion;
        lote.FechaVencimiento = vencimiento;
        lote.Cantidad = cantidad;
        lote.Estado = estado;
        await _unitOfWork.CompleteAsync();

        return lote.Id;
    }
}
