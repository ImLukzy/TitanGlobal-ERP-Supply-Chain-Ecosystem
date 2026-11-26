using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho;

public class ObtenerRutaDespachoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerRutaDespachoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RutaDespachoDetalleDto> ExecuteAsync(int id)
    {
        RutaDespachoDetalleDto? detalle = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().Where(r => r.Id == id)
            .Select(r => new RutaDespachoDetalleDto
            {
                Id = r.Id,
                Codigo = r.Codigo,
                Nombre = r.Nombre,
                Zona = r.Zona,
                Origen = r.Origen,
                Destino = r.Destino,
                Activo = r.Activo,
                DistanciaKm = r.DistanciaKm.ToString("0.00", CultureInfo.InvariantCulture),
                PeajeEstimado = r.PeajeEstimado.ToString("0.00", CultureInfo.InvariantCulture),
                Moneda = r.Moneda,
                Observaciones = r.Observaciones
            }).FirstOrDefaultAsync();
        if (detalle is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La ruta de despacho {id} no existe."); }
        return detalle;
    }
}
