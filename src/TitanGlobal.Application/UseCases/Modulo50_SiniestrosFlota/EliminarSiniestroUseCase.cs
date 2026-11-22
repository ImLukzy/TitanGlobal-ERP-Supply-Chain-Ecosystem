using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

public class EliminarSiniestroUseCase
{
    private readonly IUnitOfWork _unidad;
    public EliminarSiniestroUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task ExecuteAsync(int id)
    {
        SiniestroFlota siniestro = await ValidacionSiniestro.BuscarAsync(_unidad, id);
        if (siniestro.Estado != "Registrado")
        {
            throw ValidacionSiniestro.Conflicto("Solo se puede eliminar un siniestro registrado.");
        }
        _unidad.Repository<SiniestroFlota>().Remove(siniestro);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw ValidacionSiniestro.Conflicto("No se puede eliminar el siniestro porque está en uso.");
        }
    }
}
