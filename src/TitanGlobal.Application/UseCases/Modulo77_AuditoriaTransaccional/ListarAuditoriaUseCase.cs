using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo77_AuditoriaTransaccional;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo77_AuditoriaTransaccional;

public class ListarAuditoriaUseCase
{
    private static readonly HashSet<string> AccionesValidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Alta",
        "Modificacion",
        "Baja"
    };

    private readonly IUnitOfWork _unitOfWork;

    public ListarAuditoriaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAuditoriaDto> ExecuteAsync(
        string? entidad,
        string? entidadId,
        string? accion,
        string? usuario,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }

        if (!string.IsNullOrWhiteSpace(accion) && !AccionesValidas.Contains(accion))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La acción no es válida.");
        }

        DateTime? desde = fechaDesde.HasValue ? FechasUtc.Normalizar(fechaDesde.Value) : null;
        DateTime? hasta = fechaHasta.HasValue ? FechasUtc.Normalizar(fechaHasta.Value) : null;
        if (hasta.HasValue && hasta.Value.TimeOfDay == TimeSpan.Zero)
        {
            hasta = hasta.Value.Date.AddDays(1).AddTicks(-1);
        }
        if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha desde no puede ser posterior a la fecha hasta.");
        }

        IQueryable<RegistroAuditoria> consulta = _unitOfWork.Repository<RegistroAuditoria>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entidad))
        {
            string entidadNormalizada = entidad.Trim().ToLowerInvariant();
            consulta = consulta.Where(registro => registro.Entidad.ToLower() == entidadNormalizada);
        }

        if (!string.IsNullOrWhiteSpace(entidadId))
        {
            consulta = consulta.Where(registro => registro.EntidadId == entidadId);
        }

        if (!string.IsNullOrWhiteSpace(accion))
        {
            string accionNormalizada = accion.Trim().ToLowerInvariant();
            consulta = consulta.Where(registro => registro.Accion.ToLower() == accionNormalizada);
        }

        if (!string.IsNullOrWhiteSpace(usuario))
        {
            string usuarioNormalizado = usuario.Trim().ToLowerInvariant();
            consulta = consulta.Where(registro => registro.Usuario.ToLower() == usuarioNormalizado);
        }

        if (desde.HasValue)
        {
            consulta = consulta.Where(registro => registro.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(registro => registro.Fecha <= hasta.Value);
        }

        int total = await consulta.CountAsync();
        List<RegistroAuditoriaProyeccion> registros = await consulta
            .OrderByDescending(registro => registro.Fecha)
            .ThenByDescending(registro => registro.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(registro => new RegistroAuditoriaProyeccion
            {
                Id = registro.Id,
                Fecha = registro.Fecha,
                Entidad = registro.Entidad,
                EntidadId = registro.EntidadId,
                Accion = registro.Accion,
                Usuario = registro.Usuario,
                Ruta = registro.Ruta,
                Cambios = registro.Cambios
            })
            .ToListAsync();
        List<RegistroListadoDto> items = registros
            .Select(registro => new RegistroListadoDto
            {
                Id = registro.Id,
                Fecha = registro.Fecha,
                Entidad = registro.Entidad,
                EntidadId = registro.EntidadId,
                Accion = registro.Accion,
                Usuario = registro.Usuario,
                Ruta = registro.Ruta,
                CamposCambiados = LeerCambios(registro.Cambios).Count
            })
            .ToList();

        return new PaginaAuditoriaDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    internal static List<CambioCampoDto> LeerCambios(string cambios)
    {
        List<CambioCampoDto>? resultado = System.Text.Json.JsonSerializer.Deserialize<List<CambioCampoDto>>(
            cambios,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        return resultado ?? new List<CambioCampoDto>();
    }

    private sealed class RegistroAuditoriaProyeccion
    {
        public long Id { get; set; }

        public DateTime Fecha { get; set; }

        public string Entidad { get; set; } = string.Empty;

        public string EntidadId { get; set; } = string.Empty;

        public string Accion { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;

        public string? Ruta { get; set; }

        public string Cambios { get; set; } = string.Empty;
    }
}
