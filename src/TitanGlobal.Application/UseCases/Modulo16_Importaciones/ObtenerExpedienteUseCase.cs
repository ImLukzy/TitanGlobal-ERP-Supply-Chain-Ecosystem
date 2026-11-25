using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class ObtenerExpedienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerExpedienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ExpedienteDetalleDto> ExecuteAsync(int id)
    {
        ExpedienteDetalleDto? expediente = await _unitOfWork.Repository<ExpedienteImportacion>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new ExpedienteDetalleDto
            {
                Id = entidad.Id,
                Numero = entidad.Numero,
                OrdenAbastecimientoId = entidad.OrdenAbastecimientoId,
                OrdenNumero = entidad.OrdenAbastecimiento.Numero,
                ProveedorId = entidad.OrdenAbastecimiento.ProveedorId,
                ProveedorNombre = entidad.OrdenAbastecimiento.Proveedor.RazonSocial,
                Incoterm = entidad.Incoterm,
                MedioTransporte = entidad.MedioTransporte,
                PaisOrigen = entidad.PaisOrigen,
                LugarOrigen = entidad.LugarOrigen,
                LugarDestino = entidad.LugarDestino,
                DocumentoTransporte = entidad.DocumentoTransporte,
                FechaEmbarque = entidad.FechaEmbarque,
                FechaLlegadaEstimada = entidad.FechaLlegadaEstimada,
                FechaLlegadaReal = entidad.FechaLlegadaReal,
                Estado = entidad.Estado,
                MotivoCancelacion = entidad.MotivoCancelacion,
                Observaciones = entidad.Observaciones,
                FechaRegistro = entidad.FechaRegistro
            })
            .SingleOrDefaultAsync();
        if (expediente is null)
        {
            throw ValidacionExpedienteImportacion.NoExiste(id);
        }

        return expediente;
    }
}
