using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class ObtenerDeclaracionAduanaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerDeclaracionAduanaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeclaracionDetalleDto> ExecuteAsync(int id)
    {
        DeclaracionDetalleDto? declaracion = await _unitOfWork.Repository<DeclaracionAduana>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new DeclaracionDetalleDto
            {
                Id = entidad.Id,
                Numero = entidad.Numero,
                ExpedienteImportacionId = entidad.ExpedienteImportacionId,
                ExpedienteNumero = entidad.ExpedienteImportacion.Numero,
                AgenteAduaneroId = entidad.AgenteAduaneroId,
                AgenteRazonSocial = entidad.AgenteAduanero.RazonSocial,
                Regimen = entidad.Regimen,
                ValorAduanero = entidad.ValorAduanero,
                TasaArancelaria = entidad.TasaArancelaria,
                DerechosArancel = entidad.DerechosArancel,
                Canal = entidad.Canal,
                Estado = entidad.Estado,
                FechaNumeracion = entidad.FechaNumeracion,
                FechaLevante = entidad.FechaLevante,
                MotivoAnulacion = entidad.MotivoAnulacion,
                FechaRegistro = entidad.FechaRegistro
            })
            .SingleOrDefaultAsync();
        if (declaracion is null)
        {
            throw ValidacionDeclaracionAduana.NoExiste(id);
        }

        return declaracion;
    }
}
