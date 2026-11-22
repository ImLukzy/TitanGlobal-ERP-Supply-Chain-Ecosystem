using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

public class ObtenerSiniestroUseCase
{
    private readonly IUnitOfWork _unidad;
    public ObtenerSiniestroUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<SiniestroDetalleDto> ExecuteAsync(int id)
    {
        SiniestroFlota siniestro = await ValidacionSiniestro.BuscarAsync(_unidad, id);
        return PresentacionSiniestro.Detalle(siniestro);
    }
}
