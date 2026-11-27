using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class ActualizarAgenteAduaneroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarAgenteAduaneroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarAgenteRequestDto solicitud)
    {
        AgenteAduanero? agente = await _unitOfWork.Repository<AgenteAduanero>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (agente is null)
        {
            throw ValidacionAgenteAduanero.NoExiste(id);
        }

        ValidacionAgenteAduanero.DatosAgente datos = ValidacionAgenteAduanero.Normalizar(
            solicitud.Ruc,
            solicitud.RazonSocial,
            solicitud.NumeroRegistro,
            solicitud.Email,
            solicitud.Telefono);
        await ComprobarUnicidadAsync(id, datos);

        agente.Ruc = datos.Ruc;
        agente.RazonSocial = datos.RazonSocial;
        agente.NumeroRegistro = datos.NumeroRegistro;
        agente.Email = datos.Email;
        agente.Telefono = datos.Telefono;

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            await ComprobarUnicidadAsync(id, datos);
            throw;
        }

        return agente.Id;
    }

    private async Task ComprobarUnicidadAsync(int id, ValidacionAgenteAduanero.DatosAgente datos)
    {
        IQueryable<AgenteAduanero> agentes = _unitOfWork.Repository<AgenteAduanero>().AsQueryable();
        if (await agentes.AnyAsync(agente => agente.Id != id && agente.Ruc == datos.Ruc))
        {
            throw ValidacionAgenteAduanero.Conflicto($"Ya existe un agente con el RUC {datos.Ruc}.");
        }

        if (await agentes.AnyAsync(agente => agente.Id != id && agente.NumeroRegistro == datos.NumeroRegistro))
        {
            throw ValidacionAgenteAduanero.Conflicto(
                $"Ya existe un agente con el número de registro {datos.NumeroRegistro}.");
        }
    }
}
