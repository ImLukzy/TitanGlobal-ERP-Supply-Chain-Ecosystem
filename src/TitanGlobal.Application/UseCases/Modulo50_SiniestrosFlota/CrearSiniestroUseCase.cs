using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

public class CrearSiniestroUseCase
{
    private readonly IUnitOfWork _unidad;
    public CrearSiniestroUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(GuardarSiniestroRequestDto datos)
    {
        ValidacionSiniestro.Validar(datos);
        await ValidacionSiniestro.CodigoLibreAsync(_unidad, datos.Codigo);
        SiniestroFlota siniestro = new SiniestroFlota { Estado = "Registrado" };
        ValidacionSiniestro.Asignar(siniestro, datos);
        await _unidad.Repository<SiniestroFlota>().AddAsync(siniestro);
        await ValidacionSiniestro.GuardarAsync(_unidad, datos.Codigo);
        return siniestro.Id;
    }
}
