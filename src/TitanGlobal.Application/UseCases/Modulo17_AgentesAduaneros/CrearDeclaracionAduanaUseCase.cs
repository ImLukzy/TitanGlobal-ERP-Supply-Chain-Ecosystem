using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo16_Importaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class CrearDeclaracionAduanaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearDeclaracionAduanaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(CrearDeclaracionRequestDto solicitud)
    {
        string numero = ValidacionDeclaracionAduana.NormalizarNumero(solicitud.Numero);
        string regimen = ValidacionDeclaracionAduana.NormalizarRegimen(solicitud.Regimen);
        ValidacionDeclaracionAduana.ValidarValorAduanero(solicitud.ValorAduanero);
        ValidacionDeclaracionAduana.ValidarTasa(solicitud.TasaArancelaria);
        await ValidarExpedienteAsync(solicitud.ExpedienteImportacionId);
        await ValidarAgenteParaDeclaracion.ComprobarAsync(_unitOfWork, solicitud.AgenteAduaneroId);
        await ComprobarUnicidadAsync(numero, solicitud.ExpedienteImportacionId);

        DeclaracionAduana declaracion = new DeclaracionAduana
        {
            Numero = numero,
            ExpedienteImportacionId = solicitud.ExpedienteImportacionId,
            AgenteAduaneroId = solicitud.AgenteAduaneroId,
            Regimen = regimen,
            ValorAduanero = solicitud.ValorAduanero,
            TasaArancelaria = solicitud.TasaArancelaria,
            DerechosArancel = ValidacionDeclaracionAduana.CalcularDerechos(solicitud.ValorAduanero, solicitud.TasaArancelaria),
            Estado = ValidacionDeclaracionAduana.Registrada,
            FechaRegistro = DateTime.UtcNow
        };
        await _unitOfWork.Repository<DeclaracionAduana>().AddAsync(declaracion);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            await ComprobarUnicidadAsync(numero, solicitud.ExpedienteImportacionId);
            throw;
        }

        return declaracion.Id;
    }

    private async Task ValidarExpedienteAsync(int expedienteId)
    {
        string? estado = await _unitOfWork.Repository<ExpedienteImportacion>()
            .AsQueryable()
            .Where(expediente => expediente.Id == expedienteId)
            .Select(expediente => expediente.Estado)
            .SingleOrDefaultAsync();
        if (estado is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El expediente {expedienteId} no existe.");
        }

        if (estado != ValidacionExpedienteImportacion.EnTransito && estado != ValidacionExpedienteImportacion.EnAduana)
        {
            throw ValidacionDeclaracionAduana.Conflicto(
                $"El expediente {expedienteId} no admite declaraciones en estado {estado}.");
        }
    }

    private async Task ComprobarUnicidadAsync(string numero, int expedienteId)
    {
        IQueryable<DeclaracionAduana> declaraciones = _unitOfWork.Repository<DeclaracionAduana>().AsQueryable();
        if (await declaraciones.AnyAsync(declaracion => declaracion.Numero == numero))
        {
            throw ValidacionDeclaracionAduana.Conflicto($"Ya existe una declaración con el número {numero}.");
        }

        string? vigente = await declaraciones
            .Where(declaracion => declaracion.ExpedienteImportacionId == expedienteId
                && declaracion.Estado != ValidacionDeclaracionAduana.Anulada)
            .Select(declaracion => declaracion.Numero)
            .FirstOrDefaultAsync();
        if (vigente is not null)
        {
            throw ValidacionDeclaracionAduana.Conflicto($"El expediente {expedienteId} ya tiene la declaración {vigente}.");
        }
    }
}
