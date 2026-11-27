using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

internal static class ValidarAgenteParaDeclaracion
{
    internal static async Task ComprobarAsync(IUnitOfWork unitOfWork, int agenteId)
    {
        bool? activo = await unitOfWork.Repository<AgenteAduanero>()
            .AsQueryable()
            .Where(agente => agente.Id == agenteId)
            .Select(agente => (bool?)agente.Activo)
            .SingleOrDefaultAsync();
        if (activo is null)
        {
            throw ValidacionAgenteAduanero.NoExiste(agenteId);
        }

        if (!activo.Value)
        {
            throw ValidacionDeclaracionAduana.Conflicto($"El agente aduanero {agenteId} está inactivo.");
        }
    }
}
