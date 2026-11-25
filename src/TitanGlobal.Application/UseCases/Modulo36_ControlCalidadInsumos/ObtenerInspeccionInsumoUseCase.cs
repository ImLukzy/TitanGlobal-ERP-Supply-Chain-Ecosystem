using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

public class ObtenerInspeccionInsumoUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerInspeccionInsumoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<InspeccionInsumoDetalleDto> ExecuteAsync(int id)
    {
        IQueryable<InspeccionInsumo> consulta = _unidad.Repository<InspeccionInsumo>().AsQueryable()
            .Where(inspeccion => inspeccion.Id == id);
        FilaInspeccionInsumo? fila = await ConsultaInspeccionInsumo.Proyectar(consulta).SingleOrDefaultAsync();
        if (fila == null)
        {
            throw ValidacionInspeccionInsumo.NoEncontrada(id);
        }

        return ConsultaInspeccionInsumo.ADetalle(fila);
    }
}
