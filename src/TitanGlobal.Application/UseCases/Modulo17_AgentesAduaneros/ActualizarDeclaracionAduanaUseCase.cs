using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class ActualizarDeclaracionAduanaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarDeclaracionAduanaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, ActualizarDeclaracionRequestDto solicitud)
    {
        DeclaracionAduana? declaracion = await _unitOfWork.Repository<DeclaracionAduana>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (declaracion is null)
        {
            throw ValidacionDeclaracionAduana.NoExiste(id);
        }

        if (declaracion.Estado != ValidacionDeclaracionAduana.Registrada)
        {
            throw ValidacionDeclaracionAduana.NoRegistrada(id);
        }

        string regimen = ValidacionDeclaracionAduana.NormalizarRegimen(solicitud.Regimen);
        ValidacionDeclaracionAduana.ValidarValorAduanero(solicitud.ValorAduanero);
        ValidacionDeclaracionAduana.ValidarTasa(solicitud.TasaArancelaria);
        if (solicitud.AgenteAduaneroId != declaracion.AgenteAduaneroId)
        {
            await ValidarAgenteParaDeclaracion.ComprobarAsync(_unitOfWork, solicitud.AgenteAduaneroId);
        }

        declaracion.AgenteAduaneroId = solicitud.AgenteAduaneroId;
        declaracion.Regimen = regimen;
        declaracion.ValorAduanero = solicitud.ValorAduanero;
        declaracion.TasaArancelaria = solicitud.TasaArancelaria;
        declaracion.DerechosArancel = ValidacionDeclaracionAduana.CalcularDerechos(solicitud.ValorAduanero, solicitud.TasaArancelaria);
        await _unitOfWork.CompleteAsync();

        return declaracion.Id;
    }
}
