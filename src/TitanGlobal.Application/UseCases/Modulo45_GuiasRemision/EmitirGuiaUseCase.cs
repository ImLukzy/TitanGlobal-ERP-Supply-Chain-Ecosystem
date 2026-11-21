using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

public class EmitirGuiaUseCase
{
    private readonly IUnitOfWork _unidad;

    public EmitirGuiaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        GuiaRemision guia = await ValidacionGuia.ObtenerAsync(_unidad, id);
        ValidacionGuia.ExigirBorrador(guia, "emitir");
        guia.Estado = "Emitida";
        guia.FechaEmision = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidad.Repository<GuiaRemision>().Update(guia);
        await _unidad.CompleteAsync();
        return id;
    }
}
