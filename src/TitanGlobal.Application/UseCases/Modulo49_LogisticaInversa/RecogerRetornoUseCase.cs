using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

public class RecogerRetornoUseCase
{
    private readonly IUnitOfWork _unidad;
    public RecogerRetornoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id, RegistrarCantidadRetornoRequestDto datos)
    {
        RetornoLogistico retorno = await ValidacionRetorno.BuscarAsync(_unidad, id);
        if (retorno.Estado != "Programado")
        {
            throw ValidacionRetorno.Conflicto("Solo se puede recoger un retorno programado.");
        }
        ValidacionRetorno.Cantidad(datos.Cantidad, "cantidad");
        if (datos.Cantidad > retorno.CantidadProgramada)
        {
            throw ValidacionRetorno.Error("La cantidad recogida no puede superar la programada.");
        }
        string responsable = ValidacionRetorno.Texto(datos.Responsable, "responsable", 100);
        retorno.CantidadRecogida = datos.Cantidad;
        retorno.FechaRecogida = FechasUtc.Normalizar(DateTime.UtcNow);
        retorno.ResponsableRecogida = responsable;
        retorno.Estado = "Recogido";
        await _unidad.CompleteAsync();
        return id;
    }
}
