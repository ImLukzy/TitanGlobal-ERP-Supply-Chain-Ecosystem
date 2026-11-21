using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

public class ActualizarGuiaUseCase
{
    private readonly IUnitOfWork _unidad;

    public ActualizarGuiaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id, GuardarGuiaRequestDto solicitud)
    {
        GuiaRemision guia = await ValidacionGuia.ObtenerAsync(_unidad, id);
        ValidacionGuia.ExigirBorrador(guia, "editar");
        GuardarGuiaRequestDto datos = ValidacionGuia.Normalizar(solicitud);
        await ValidacionGuia.ExigirNumeroLibreAsync(_unidad, datos.Serie, datos.Numero, id);
        foreach (LineaGuiaRemision anterior in guia.Lineas.ToList())
        {
            _unidad.Repository<LineaGuiaRemision>().Remove(anterior);
        }
        ValidacionGuia.Asignar(guia, datos);
        for (int i = 0; i < datos.Lineas!.Count; i++)
        {
            LineaGuiaRemision nueva = ValidacionGuia.Entidad(datos.Lineas[i], i + 1);
            nueva.GuiaRemisionId = id;
            await _unidad.Repository<LineaGuiaRemision>().AddAsync(nueva);
        }
        _unidad.Repository<GuiaRemision>().Update(guia);
        await ValidacionGuia.GuardarAsync(_unidad, datos.Serie, datos.Numero);
        return id;
    }
}
