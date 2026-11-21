using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

public class EliminarEntregaUseCase
{
    private readonly IUnitOfWork _unidad;

    public EliminarEntregaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task ExecuteAsync(int id)
    {
        EntregaDespacho entrega = await ValidacionEntrega.ObtenerAsync(_unidad, id);
        ValidacionEntrega.ExigirProgramada(entrega, "eliminar");
        _unidad.Repository<EntregaDespacho>().Remove(entrega);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw ValidacionEntrega.Conflicto("No se puede eliminar la entrega porque está en uso.");
        }
    }
}
