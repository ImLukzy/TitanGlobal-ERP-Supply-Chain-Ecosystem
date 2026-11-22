using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

public class RecibirRetornoUseCase
{
    private readonly IUnitOfWork _unidad;
    public RecibirRetornoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id, RegistrarCantidadRetornoRequestDto datos)
    {
        RetornoLogistico retorno = await ValidacionRetorno.BuscarAsync(_unidad, id);
        if (retorno.Estado != "Recogido")
        {
            throw ValidacionRetorno.Conflicto("Solo se puede recibir un retorno recogido.");
        }
        ValidacionRetorno.Cantidad(datos.Cantidad, "cantidad");
        if (datos.Cantidad > retorno.CantidadRecogida!.Value)
        {
            throw ValidacionRetorno.Error("La cantidad recibida no puede superar la recogida.");
        }
        string responsable = ValidacionRetorno.Texto(datos.Responsable, "responsable", 100);
        retorno.CantidadRecibida = datos.Cantidad;
        retorno.FechaRecepcion = FechasUtc.Normalizar(DateTime.UtcNow);
        retorno.ResponsableRecepcion = responsable;
        retorno.Estado = "Recibido";
        await _unidad.CompleteAsync();
        return id;
    }
}
