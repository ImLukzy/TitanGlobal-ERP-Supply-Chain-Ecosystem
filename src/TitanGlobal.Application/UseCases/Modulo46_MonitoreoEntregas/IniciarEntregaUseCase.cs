using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

public class IniciarEntregaUseCase
{
    private readonly IUnitOfWork _unidad;

    public IniciarEntregaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        EntregaDespacho entrega = await ValidacionEntrega.ObtenerAsync(_unidad, id);
        ValidacionEntrega.ExigirProgramada(entrega, "iniciar");
        DateTime fecha = FechasUtc.Normalizar(DateTime.UtcNow);
        entrega.Estado = "EnRuta";
        entrega.FechaSalida = fecha;
        await ValidacionEntrega.GuardarTransicionAsync(_unidad, entrega, fecha, "Salida a ruta registrada.");
        return id;
    }
}
