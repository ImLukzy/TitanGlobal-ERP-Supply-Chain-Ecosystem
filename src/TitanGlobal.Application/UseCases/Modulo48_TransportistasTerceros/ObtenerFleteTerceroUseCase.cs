using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

public class ObtenerFleteTerceroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerFleteTerceroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FleteTerceroDetalleDto> ExecuteAsync(int id)
    {
        FleteTerceroDetalleDto? detalle = await _unitOfWork.Repository<FleteTercero>().AsQueryable().Where(f => f.Id == id)
            .Select(f => new FleteTerceroDetalleDto
            {
                Id = f.Id,
                TransportistaId = f.TransportistaTerceroId,
                TransportistaRazonSocial = f.TransportistaTercero.RazonSocial,
                Referencia = f.Referencia,
                Fecha = f.Fecha,
                Origen = f.Origen,
                Destino = f.Destino,
                Importe = f.Importe.ToString("0.00", CultureInfo.InvariantCulture),
                Moneda = f.Moneda,
                Estado = f.Estado,
                Observaciones = f.Observaciones,
                MotivoCancelacion = f.MotivoCancelacion,
                FechaCancelacion = f.FechaCancelacion
            }).FirstOrDefaultAsync();
        if (detalle is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El flete {id} no existe."); }
        return detalle;
    }
}
