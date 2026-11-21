using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

public class CrearEntregaUseCase
{
    private readonly IUnitOfWork _unidad;

    public CrearEntregaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(GuardarEntregaRequestDto solicitud)
    {
        GuardarEntregaRequestDto datos = ValidacionEntrega.Normalizar(solicitud);
        await ValidacionEntrega.ExigirCodigoLibreAsync(_unidad, datos.Codigo);
        EntregaDespacho entrega = new EntregaDespacho { Estado = "Programada" };
        ValidacionEntrega.Asignar(entrega, datos);
        entrega.Eventos.Add(new EventoEntrega
        {
            Estado = "Programada",
            Fecha = FechasUtc.Normalizar(DateTime.UtcNow),
            Descripcion = "Entrega programada."
        });
        await _unidad.Repository<EntregaDespacho>().AddAsync(entrega);
        await ValidacionEntrega.GuardarAsync(_unidad, datos.Codigo);
        return entrega.Id;
    }
}
