using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo78_NotificacionesAlertas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo78_NotificacionesAlertas;

public class AlertasUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AlertasUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAlertasDto> ListarAsync(
        string? tipo,
        string? severidad,
        bool incluirDescartadas,
        int pagina,
        int tamanoPagina)
    {
        ValidarPaginacion(pagina, tamanoPagina);
        string? tipoNormalizado = ValidarTipo(tipo);
        string? severidadNormalizada = ValidarSeveridad(severidad);
        List<AlertaDto> alertas = await ObtenerAlertasAsync();
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
        if (!incluirDescartadas)
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
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();
        return new PaginaAlertasDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ResumenAlertasDto> ResumenAsync()
    {
        List<AlertaDto> alertas = await ObtenerAlertasAsync();
        List<string> clavesDescartadas = await _unitOfWork.Repository<AlertaDescartada>()
            .AsQueryable()
            .Select(alerta => alerta.Clave)
            .ToListAsync();
        HashSet<string> conjuntoDescartadas = clavesDescartadas.ToHashSet(StringComparer.Ordinal);
        int descartadas = 0;
        foreach (AlertaDto alerta in alertas)
        {
            alerta.Descartada = conjuntoDescartadas.Contains(alerta.Clave);
            if (alerta.Descartada)
            {
                descartadas++;
            }
        }

        List<AlertaDto> activas = alertas.Where(alerta => !alerta.Descartada).ToList();
        List<TipoAlertasDto> porTipo = new List<TipoAlertasDto>();
        foreach (string tipo in GeneradorAlertas.ObtenerTipos())
        {
            int cantidad = activas.Count(alerta => alerta.Tipo == tipo);
            if (cantidad > 0)
            {
                porTipo.Add(new TipoAlertasDto { Tipo = tipo, Alertas = cantidad });
            }
        }

        return new ResumenAlertasDto
        {
            Total = activas.Count,
            Altas = activas.Count(alerta => alerta.Severidad == "Alta"),
            Medias = activas.Count(alerta => alerta.Severidad == "Media"),
            Bajas = activas.Count(alerta => alerta.Severidad == "Baja"),
            Descartadas = descartadas,
            PorTipo = porTipo
        };
    }

    public async Task DescartarAsync(ClaveAlertaRequestDto solicitud)
    {
        string clave = ValidarClave(solicitud.Clave);
        List<AlertaDto> alertas = await ObtenerAlertasAsync();
        if (!alertas.Any(alerta => alerta.Clave == clave))
        {
            throw new ReglaNegocioException(404, $"La alerta {clave} no existe o ya no está activa.");
        }

        AlertaDescartada? existente = await _unitOfWork.Repository<AlertaDescartada>()
            .AsQueryable()
            .SingleOrDefaultAsync(alerta => alerta.Clave == clave);
        if (existente is not null)
        {
            throw new ReglaNegocioException(409, $"La alerta {clave} ya está descartada.");
        }

        AlertaDescartada descartada = new AlertaDescartada
        {
            Clave = clave,
            FechaDescarte = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };
        try
        {
            await _unitOfWork.Repository<AlertaDescartada>().AddAsync(descartada);
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsClaveDuplicada(excepcion))
        {
            throw new ReglaNegocioException(409, $"La alerta {clave} ya está descartada.");
        }
    }

    public async Task RestaurarAsync(ClaveAlertaRequestDto solicitud)
    {
        string clave = ValidarClave(solicitud.Clave);
        AlertaDescartada? descartada = await _unitOfWork.Repository<AlertaDescartada>()
            .AsQueryable()
            .SingleOrDefaultAsync(alerta => alerta.Clave == clave);
        if (descartada is null)
        {
            throw new ReglaNegocioException(404, $"La alerta {clave} no está descartada.");
        }

        _unitOfWork.Repository<AlertaDescartada>().Remove(descartada);
        await _unitOfWork.CompleteAsync();
    }

    private async Task<List<AlertaDto>> ObtenerAlertasAsync()
    {
        return await GeneradorAlertas.GenerarAsync(_unitOfWork);
    }

    private static string ValidarClave(string? claveEntrada)
    {
        string clave = claveEntrada?.Trim() ?? string.Empty;
        if (clave.Length == 0)
        {
            throw new ReglaNegocioException(400, "La clave de la alerta es obligatoria.");
        }

        if (clave.Length > 100)
        {
            throw new ReglaNegocioException(400, "La clave de la alerta no puede superar 100 caracteres.");
        }

        return clave;
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

    private static bool EsClaveDuplicada(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
