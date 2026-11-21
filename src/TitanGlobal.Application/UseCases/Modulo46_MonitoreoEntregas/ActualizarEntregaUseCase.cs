using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

public class ActualizarEntregaUseCase
{
    private readonly IUnitOfWork _unidad;

    public ActualizarEntregaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id, GuardarEntregaRequestDto solicitud)
    {
        EntregaDespacho entrega = await ValidacionEntrega.ObtenerAsync(_unidad, id);
        ValidacionEntrega.ExigirProgramada(entrega, "editar");
        GuardarEntregaRequestDto datos = ValidacionEntrega.Normalizar(solicitud);
        await ValidacionEntrega.ExigirCodigoLibreAsync(_unidad, datos.Codigo, id);
        ValidacionEntrega.Asignar(entrega, datos);
        _unidad.Repository<EntregaDespacho>().Update(entrega);
        await ValidacionEntrega.GuardarAsync(_unidad, datos.Codigo);
        return id;
    }
}
