using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo77_AuditoriaTransaccional;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo77_AuditoriaTransaccional.Queries;

public class ListarAuditoriaQuery : IRequest<PaginaAuditoriaDto>
{
    public string? Entidad { get; set; }

    public string? EntidadId { get; set; }

    public string? Accion { get; set; }

    public string? Usuario { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarAuditoriaQueryHandler : IRequestHandler<ListarAuditoriaQuery, PaginaAuditoriaDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarAuditoriaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAuditoriaDto> Handle(ListarAuditoriaQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }

        if (!string.IsNullOrWhiteSpace(request.Accion) && !AccionesValidas.Contains(request.Accion))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La acción no es válida.");
        }

        DateTime? desde = request.FechaDesde.HasValue ? FechasUtc.Normalizar(request.FechaDesde.Value) : null;
        DateTime? hasta = request.FechaHasta.HasValue ? FechasUtc.Normalizar(request.FechaHasta.Value) : null;
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
        if (!string.IsNullOrWhiteSpace(request.Entidad))
        {
            string entidadNormalizada = request.Entidad.Trim().ToLowerInvariant();
            consulta = consulta.Where(registro => registro.Entidad.ToLower() == entidadNormalizada);
        }

        if (!string.IsNullOrWhiteSpace(request.EntidadId))
        {
            consulta = consulta.Where(registro => registro.EntidadId == request.EntidadId);
        }

        if (!string.IsNullOrWhiteSpace(request.Accion))
        {
            string accionNormalizada = request.Accion.Trim().ToLowerInvariant();
            consulta = consulta.Where(registro => registro.Accion.ToLower() == accionNormalizada);
        }

        if (!string.IsNullOrWhiteSpace(request.Usuario))
        {
            string usuarioNormalizado = request.Usuario.Trim().ToLowerInvariant();
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
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
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
                CamposCambiados = OperacionesAuditoria.LeerCambios(registro.Cambios).Count
            })
            .ToList();

        return new PaginaAuditoriaDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static readonly HashSet<string> AccionesValidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Alta",
        "Modificacion",
        "Baja"
    };

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
