using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Queries;

public class ObtenerAusenciaQuery : IRequest<AusenciaDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerAusenciaQueryHandler : IRequestHandler<ObtenerAusenciaQuery, AusenciaDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerAusenciaQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<AusenciaDetalleDto> Handle(ObtenerAusenciaQuery request, CancellationToken cancellationToken)
    {
        AusenciaDetalleDto? detalle = await _unidad.Repository<Ausencia>().AsQueryable()
            .Where(ausencia => ausencia.Id == request.Id)
            .Select(ausencia => new AusenciaDetalleDto
            {
                Id = ausencia.Id,
                Numero = ausencia.Numero,
                EmpleadoId = ausencia.EmpleadoId,
                EmpleadoLegajo = ausencia.Empleado.Legajo,
                EmpleadoNombre = ausencia.Empleado.Apellidos + ", " + ausencia.Empleado.Nombres,
                Tipo = ausencia.Tipo,
                FechaInicio = ausencia.FechaInicio,
                FechaFin = ausencia.FechaFin,
                Dias = ausencia.Dias,
                Estado = ausencia.Estado,
                FechaSolicitud = ausencia.FechaSolicitud,
                Motivo = ausencia.Motivo,
                DecididoPor = ausencia.DecididoPor,
                FechaDecision = ausencia.FechaDecision,
                MotivoRechazo = ausencia.MotivoRechazo,
                FechaCancelacion = ausencia.FechaCancelacion
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw ValidacionAusencia.AusenciaInexistente(request.Id);
        }

        return detalle;
    }
}
