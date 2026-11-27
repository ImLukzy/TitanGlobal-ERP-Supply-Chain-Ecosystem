using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo78_NotificacionesAlertas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo78_NotificacionesAlertas.Queries;

public class ListarAlertasQuery : IRequest<PaginaAlertasDto>
{
    public string? Tipo { get; set; }

    public string? Severidad { get; set; }

    public bool IncluirDescartadas { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarAlertasQueryHandler : IRequestHandler<ListarAlertasQuery, PaginaAlertasDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarAlertasQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAlertasDto> Handle(ListarAlertasQuery request, CancellationToken cancellationToken)
    {
        ValidarPaginacion(request.Pagina, request.TamanoPagina);
        string? tipoNormalizado = ValidarTipo(request.Tipo);
        string? severidadNormalizada = ValidarSeveridad(request.Severidad);
        List<AlertaDto> alertas = await OperacionesAlertas.ObtenerAlertasAsync(_unitOfWork);
        List<string> clavesDescartadas = await _unitOfWork.Repository<AlertaDescartada>()
            .AsQueryable()
            .Select(alerta => alerta.Clave)
            .ToListAsync();
        HashSet<string> conjuntoDescartadas = clavesDescartadas.ToHashSet(StringComparer.Ordinal);
        foreach (AlertaDto alerta in alertas)
        {
            alerta.Descartada = conjuntoDescartadas.Contains(alerta.Clave);
        }

        IEnumerable<AlertaDto> consulta = alertas;
        if (!request.IncluirDescartadas)
        {
            consulta = consulta.Where(alerta => !alerta.Descartada);
        }

        if (tipoNormalizado is not null)
        {
            consulta = consulta.Where(alerta => alerta.Tipo == tipoNormalizado);
        }

        if (severidadNormalizada is not null)
        {
            consulta = consulta.Where(alerta => alerta.Severidad == severidadNormalizada);
        }

        List<AlertaDto> ordenadas = consulta
            .OrderBy(alerta => alerta.Severidad == "Alta" ? 0 : alerta.Severidad == "Media" ? 1 : 2)
            .ThenBy(alerta => alerta.FechaReferencia)
            .ThenBy(alerta => alerta.Clave, StringComparer.Ordinal)
            .ToList();
        int total = ordenadas.Count;
        List<AlertaDto> items = ordenadas
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .ToList();
        return new PaginaAlertasDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static string? ValidarTipo(string? tipoEntrada)
    {
        string tipo = tipoEntrada?.Trim() ?? string.Empty;
        if (tipo.Length == 0)
        {
            return null;
        }

        string? tipoValido = GeneradorAlertas.ObtenerTipos()
            .SingleOrDefault(valor => string.Equals(valor, tipo, StringComparison.OrdinalIgnoreCase));
        if (tipoValido is null)
        {
            throw new ReglaNegocioException(400, "El tipo de alerta no es válido.");
        }

        return tipoValido;
    }

    private static string? ValidarSeveridad(string? severidadEntrada)
    {
        string severidad = severidadEntrada?.Trim() ?? string.Empty;
        if (severidad.Length == 0)
        {
            return null;
        }

        string? severidadValida = new[] { "Alta", "Media", "Baja" }
            .SingleOrDefault(valor => string.Equals(valor, severidad, StringComparison.OrdinalIgnoreCase));
        if (severidadValida is null)
        {
            throw new ReglaNegocioException(400, "La severidad no es válida.");
        }

        return severidadValida;
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
