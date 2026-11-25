using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class ListarAgentesAduanerosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarAgentesAduanerosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAgentesDto> ExecuteAsync(string? busqueda, bool? activo, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionAgenteAduanero.Error("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionAgenteAduanero.Error("El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<AgenteAduanero> consulta = _unitOfWork.Repository<AgenteAduanero>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (busquedaNormalizada.Length > 0)
        {
            consulta = consulta.Where(agente =>
                agente.Ruc.ToLower().Contains(busquedaNormalizada)
                || agente.RazonSocial.ToLower().Contains(busquedaNormalizada));
        }

        if (activo is not null)
        {
            consulta = consulta.Where(agente => agente.Activo == activo.Value);
        }

        IQueryable<DeclaracionAduana> declaraciones = _unitOfWork.Repository<DeclaracionAduana>().AsQueryable();
        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<AgenteDto> items = await consulta
            .OrderBy(agente => agente.RazonSocial)
            .ThenBy(agente => agente.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(agente => new AgenteDto
            {
                Id = agente.Id,
                Ruc = agente.Ruc,
                RazonSocial = agente.RazonSocial,
                NumeroRegistro = agente.NumeroRegistro,
                Email = agente.Email,
                Telefono = agente.Telefono,
                Activo = agente.Activo,
                TotalDeclaraciones = declaraciones.Count(declaracion => declaracion.AgenteAduaneroId == agente.Id),
                FechaRegistro = agente.FechaRegistro
            })
            .ToListAsync();

        return new PaginaAgentesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
