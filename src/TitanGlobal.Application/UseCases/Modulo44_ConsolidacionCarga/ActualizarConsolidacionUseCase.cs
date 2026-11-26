using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;

public class ActualizarConsolidacionUseCase
{
    private readonly IUnitOfWork _unidad;
    public ActualizarConsolidacionUseCase(IUnitOfWork unidad)
    { _unidad = unidad; }
    public async Task<int> ExecuteAsync(int id, GuardarConsolidacionRequestDto solicitud)
    {
        ConsolidacionCarga carga = await ValidacionCarga.ObtenerAsync(_unidad, id);
        ValidacionCarga.ExigirBorrador(carga, "editar");
        GuardarConsolidacionRequestDto datos = ValidacionCarga.Normalizar(solicitud);
        string codigo = datos.Codigo.ToLowerInvariant();
        if (await _unidad.Repository<ConsolidacionCarga>().AsQueryable().AnyAsync(c => c.Id != id && c.Codigo.Trim().ToLower() == codigo)) { throw ValidacionCarga.Duplicado(datos.Codigo); }
        foreach (LineaConsolidacionCarga anterior in carga.Lineas.ToList()) { _unidad.Repository<LineaConsolidacionCarga>().Remove(anterior); }
        carga.Codigo = datos.Codigo; carga.Nombre = datos.Nombre; carga.Observaciones = datos.Observaciones;
        for (int i = 0; i < datos.Lineas!.Count; i++) { LineaConsolidacionCarga nueva = ValidacionCarga.Entidad(datos.Lineas[i], i + 1); nueva.ConsolidacionCargaId = id; await _unidad.Repository<LineaConsolidacionCarga>().AddAsync(nueva); }
        _unidad.Repository<ConsolidacionCarga>().Update(carga);
        await ValidacionCarga.GuardarAsync(_unidad, datos.Codigo);
        return carga.Id;
    }
}
