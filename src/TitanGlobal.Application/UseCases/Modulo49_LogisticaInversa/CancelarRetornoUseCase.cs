using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

public class CancelarRetornoUseCase
{
    private readonly IUnitOfWork _unidad;
    public CancelarRetornoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id, CancelarRetornoRequestDto datos)
    {
        RetornoLogistico retorno = await ValidacionRetorno.BuscarAsync(_unidad, id);
        if (retorno.Estado == "Cancelado")
        {
            throw ValidacionRetorno.Conflicto("El retorno ya está cancelado.");
        }
        if (retorno.Estado != "Programado" && retorno.Estado != "Recogido")
        {
            throw ValidacionRetorno.Conflicto("Solo se puede cancelar un retorno programado o recogido.");
        }
        string motivo = datos.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw ValidacionRetorno.Error("El motivo de cancelación es obligatorio.");
        }
        if (motivo.Length > 500)
        {
            throw ValidacionRetorno.Error("El motivo de cancelación no puede superar 500 caracteres.");
        }
        retorno.MotivoCancelacion = motivo;
        retorno.FechaCancelacion = FechasUtc.Normalizar(DateTime.UtcNow);
        retorno.Estado = "Cancelado";
        await _unidad.CompleteAsync();
        return id;
    }
}
