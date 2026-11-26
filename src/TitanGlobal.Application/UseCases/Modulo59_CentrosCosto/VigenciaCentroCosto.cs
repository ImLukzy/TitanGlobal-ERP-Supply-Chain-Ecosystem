using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo59_CentrosCosto;

internal static class VigenciaCentroCosto
{
    internal static Task<CentroCostoDetalleDto> CambiarAsync(IUnitOfWorkCentrosCosto unitOfWork, int id, bool activo)
    {
        return unitOfWork.ConCentrosCostoBloqueadosAsync(async () =>
        {
            IQueryable<CentroCosto> consulta = unitOfWork.Repository<CentroCosto>().AsQueryable();
            CentroCosto centro = await ConsultasCentroCosto.CargarParaEscrituraAsync(consulta, id);
            if (centro.Activo == activo)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "El centro de costo ya tiene el estado solicitado.");
            }

            if (activo)
            {
                await ValidacionCentroCosto.ValidarAncestrosAsync(consulta.AsNoTracking(), id, centro.CentroCostoPadreId, exigirPadreActivo: true);
            }
            else if (await consulta.AnyAsync(hijo => hijo.CentroCostoPadreId == id && hijo.Activo))
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede desactivar un centro de costo con hijos activos.");
            }

            centro.Activo = activo;
            centro.FechaDesactivacion = activo ? null : FechasUtc.Normalizar(DateTime.UtcNow);
            await ValidacionCentroCosto.GuardarAsync(unitOfWork);
            return await ConsultasCentroCosto.ObtenerDetalleAsync(consulta, id);
        });
    }
}
