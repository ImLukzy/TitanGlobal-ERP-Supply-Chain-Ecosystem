using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public class DescartarSugerenciaReabastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public DescartarSugerenciaReabastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EjecucionReabastecimientoItemDto> ExecuteAsync(
        int id,
        DescartarSugerenciaRequestDto? solicitud)
    {
        string? motivo = ValidacionReabastecimiento.NormalizarMotivoDescarte(solicitud?.Motivo);

        EjecucionReabastecimiento? ejecucionEncontrada = await _unitOfWork.Repository<EjecucionReabastecimiento>()
            .AsQueryable()
            .Include(registro => registro.ReglaReabastecimiento)
                .ThenInclude(regla => regla.Producto)
            .Include(registro => registro.ReglaReabastecimiento)
                .ThenInclude(regla => regla.Almacen)
            .Include(registro => registro.ReglaReabastecimiento)
                .ThenInclude(regla => regla.ProveedorPreferido)
            .Include(registro => registro.SolicitudCompra)
            .SingleOrDefaultAsync(registro => registro.Id == id);

        EjecucionReabastecimiento ejecucion = ValidacionReabastecimiento.ValidarEjecucionExistente(ejecucionEncontrada, id);
        ValidacionReabastecimiento.ValidarEjecucionPendiente(ejecucion);

        ejecucion.Estado = "Descartada";
        if (motivo is not null)
        {
            ejecucion.Observacion = motivo;
        }
        else
        {
            ejecucion.Observacion = "Sugerencia descartada por el usuario.";
        }

        await _unitOfWork.CompleteAsync();

        return ValidacionReabastecimiento.MapearEjecucion(ejecucion);
    }
}
