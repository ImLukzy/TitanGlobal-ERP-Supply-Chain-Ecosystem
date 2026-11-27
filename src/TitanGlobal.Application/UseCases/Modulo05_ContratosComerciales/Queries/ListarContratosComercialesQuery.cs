using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Queries;

public class ListarContratosComercialesQuery : IRequest<PaginaContratosDto>
{
    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public int? ClienteId { get; set; }

    public bool? Vencidos { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarContratosComercialesQueryHandler : IRequestHandler<ListarContratosComercialesQuery, PaginaContratosDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarContratosComercialesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaContratosDto> Handle(ListarContratosComercialesQuery request, CancellationToken cancellationToken)
    {
        ValidarPaginacion(request.Pagina, request.TamanoPagina);
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        string[] estadosValidos = { "Borrador", "Vigente", "Finalizado", "Cancelado" };
        if (estadoNormalizado.Length > 0 && !estadosValidos.Any(valor =>
            string.Equals(valor, estadoNormalizado, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ReglaNegocioException(400, "El estado no es válido.");
        }

        IQueryable<ContratoComercial> consulta = _unitOfWork.Repository<ContratoComercial>().AsQueryable();
        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (busquedaNormalizada.Length > 0)
        {
            consulta = consulta.Where(contrato => contrato.Numero.ToLower().Contains(busquedaNormalizada)
                || contrato.Titulo.ToLower().Contains(busquedaNormalizada)
                || contrato.Cliente.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(contrato => contrato.Estado.ToLower() == estadoNormalizado.ToLower());
        }

        if (request.ClienteId.HasValue)
        {
            consulta = consulta.Where(contrato => contrato.ClienteId == request.ClienteId.Value);
        }

        if (request.Vencidos == true)
        {
            DateTime hoy = DateTime.UtcNow.Date;
            consulta = consulta.Where(contrato => contrato.Estado == "Vigente" && contrato.FechaFin < hoy);
        }

        int total = await consulta.CountAsync();
        List<ContratoProyeccionDto> proyecciones = await consulta
            .OrderByDescending(contrato => contrato.FechaInicio)
            .ThenByDescending(contrato => contrato.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(contrato => new ContratoProyeccionDto
            {
                Id = contrato.Id,
                Numero = contrato.Numero,
                ClienteId = contrato.ClienteId,
                ClienteNombre = contrato.Cliente.Nombre,
                Titulo = contrato.Titulo,
                FechaInicio = contrato.FechaInicio,
                FechaFin = contrato.FechaFin,
                Estado = contrato.Estado,
                MontoComprometido = contrato.MontoComprometido,
                MontoConsumido = _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                    .Where(orden => orden.ClienteId == contrato.ClienteId
                        && orden.Fecha >= contrato.FechaInicio
                        && orden.Fecha < contrato.FechaFin.AddDays(1))
                    .Sum(orden => (decimal?)orden.Total) ?? 0m
            })
            .ToListAsync();
        // El consumo se resuelve mediante una subconsulta correlacionada, sin consultas por contrato.

        List<ContratoListadoDto> items = proyecciones.Select(proyeccion => OperacionesContratosComerciales.MapearListado(proyeccion)).ToList();
        return new PaginaContratosDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(400, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(400, "El tamaño de página debe estar entre 1 y 100.");
        }
    }
}
