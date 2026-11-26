using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

public class EliminarGuiaUseCase
{
    private readonly IUnitOfWork _unidad;

    public EliminarGuiaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task ExecuteAsync(int id)
    {
        GuiaRemision guia = await ValidacionGuia.ObtenerAsync(_unidad, id);
        ValidacionGuia.ExigirBorrador(guia, "eliminar");
        _unidad.Repository<GuiaRemision>().Remove(guia);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede eliminar la guía porque está en uso.");
        }
    }
}
