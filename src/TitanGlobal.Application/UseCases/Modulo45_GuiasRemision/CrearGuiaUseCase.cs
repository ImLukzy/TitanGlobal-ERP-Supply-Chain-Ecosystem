using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

public class CrearGuiaUseCase
{
    private readonly IUnitOfWork _unidad;

    public CrearGuiaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(GuardarGuiaRequestDto solicitud)
    {
        GuardarGuiaRequestDto datos = ValidacionGuia.Normalizar(solicitud);
        await ValidacionGuia.ExigirNumeroLibreAsync(_unidad, datos.Serie, datos.Numero);
        GuiaRemision guia = new GuiaRemision { Estado = "Borrador" };
        ValidacionGuia.Asignar(guia, datos);
        for (int i = 0; i < datos.Lineas!.Count; i++)
        {
            guia.Lineas.Add(ValidacionGuia.Entidad(datos.Lineas[i], i + 1));
        }
        await _unidad.Repository<GuiaRemision>().AddAsync(guia);
        await ValidacionGuia.GuardarAsync(_unidad, datos.Serie, datos.Numero);
        return guia.Id;
    }
}
