using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class CrearAgenteAduaneroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearAgenteAduaneroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarAgenteRequestDto solicitud)
    {
        ValidacionAgenteAduanero.DatosAgente datos = ValidacionAgenteAduanero.Normalizar(
            solicitud.Ruc,
            solicitud.RazonSocial,
            solicitud.NumeroRegistro,
            solicitud.Email,
            solicitud.Telefono);
        await ComprobarUnicidadAsync(datos);

        AgenteAduanero agente = new AgenteAduanero
        {
            Ruc = datos.Ruc,
            RazonSocial = datos.RazonSocial,
            NumeroRegistro = datos.NumeroRegistro,
            Email = datos.Email,
            Telefono = datos.Telefono,
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        await _unitOfWork.Repository<AgenteAduanero>().AddAsync(agente);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            await ComprobarUnicidadAsync(datos);
            throw;
        }

        return agente.Id;
    }

    private async Task ComprobarUnicidadAsync(ValidacionAgenteAduanero.DatosAgente datos)
    {
        IQueryable<AgenteAduanero> agentes = _unitOfWork.Repository<AgenteAduanero>().AsQueryable();
        if (await agentes.AnyAsync(agente => agente.Ruc == datos.Ruc))
        {
            throw ValidacionAgenteAduanero.Conflicto($"Ya existe un agente con el RUC {datos.Ruc}.");
        }

        if (await agentes.AnyAsync(agente => agente.NumeroRegistro == datos.NumeroRegistro))
        {
            throw ValidacionAgenteAduanero.Conflicto(
                $"Ya existe un agente con el número de registro {datos.NumeroRegistro}.");
        }
    }
}
