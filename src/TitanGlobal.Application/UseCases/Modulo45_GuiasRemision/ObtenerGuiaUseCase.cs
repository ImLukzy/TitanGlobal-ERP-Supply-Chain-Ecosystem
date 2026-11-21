using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

public class ObtenerGuiaUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerGuiaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<GuiaDetalleDto> ExecuteAsync(int id)
    {
        GuiaRemision guia = await ValidacionGuia.ObtenerAsync(_unidad, id);
        return PresentacionGuia.Detalle(guia);
    }
}
