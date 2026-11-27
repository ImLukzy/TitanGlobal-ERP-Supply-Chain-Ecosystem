using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class ObtenerAgenteAduaneroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerAgenteAduaneroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AgenteDto> ExecuteAsync(int id)
    {
        IQueryable<DeclaracionAduana> declaraciones = _unitOfWork.Repository<DeclaracionAduana>().AsQueryable();
        AgenteDto? agente = await _unitOfWork.Repository<AgenteAduanero>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new AgenteDto
            {
                Id = entidad.Id,
                Ruc = entidad.Ruc,
                RazonSocial = entidad.RazonSocial,
                NumeroRegistro = entidad.NumeroRegistro,
                Email = entidad.Email,
                Telefono = entidad.Telefono,
                Activo = entidad.Activo,
                TotalDeclaraciones = declaraciones.Count(declaracion => declaracion.AgenteAduaneroId == entidad.Id),
                FechaRegistro = entidad.FechaRegistro
            })
            .SingleOrDefaultAsync();
        if (agente is null)
        {
            throw ValidacionAgenteAduanero.NoExiste(id);
        }

        return agente;
    }
}
