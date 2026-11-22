using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

public class ActualizarSiniestroUseCase
{
    private readonly IUnitOfWork _unidad;
    public ActualizarSiniestroUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id, GuardarSiniestroRequestDto datos)
    {
        SiniestroFlota siniestro = await ValidacionSiniestro.BuscarAsync(_unidad, id);
        if (siniestro.Estado != "Registrado")
        {
            throw ValidacionSiniestro.Conflicto("Solo se puede editar un siniestro registrado.");
        }
        ValidacionSiniestro.Validar(datos);
        await ValidacionSiniestro.CodigoLibreAsync(_unidad, datos.Codigo, id);
        ValidacionSiniestro.Asignar(siniestro, datos);
        await ValidacionSiniestro.GuardarAsync(_unidad, datos.Codigo);
        return id;
    }
}
