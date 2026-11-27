using MediatR;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Queries;

public class ObtenerIncidenteSstQuery : IRequest<IncidenteSstDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerIncidenteSstQueryHandler : IRequestHandler<ObtenerIncidenteSstQuery, IncidenteSstDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerIncidenteSstQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<IncidenteSstDetalleDto> Handle(ObtenerIncidenteSstQuery request, CancellationToken cancellationToken)
    {
        IncidenteSst incidente = await ValidacionSst.ObtenerIncidenteAsync(_unidad, request.Id);
        return ConvertirDetalle(incidente);
    }

    private static IncidenteSstDetalleDto ConvertirDetalle(IncidenteSst incidente)
    {
        IncidenteSstDetalleDto detalle = new IncidenteSstDetalleDto
        {
            Id = incidente.Id,
            Numero = incidente.Numero,
            EmpleadoId = incidente.EmpleadoId,
            Empleado = ValidacionSst.NombreEmpleado(incidente.Empleado),
            Tipo = incidente.Tipo,
            Severidad = incidente.Severidad,
            Fecha = incidente.Fecha,
            Lugar = incidente.Lugar,
            Descripcion = incidente.Descripcion,
            DiasBaja = incidente.DiasBaja,
            Estado = incidente.Estado,
            FechaCierre = incidente.FechaCierre
        };
        foreach (AccionCorrectivaSst accion in incidente.AccionesCorrectivasSst.OrderBy(otra => otra.Id))
        {
            detalle.Acciones.Add(new AccionCorrectivaSstDto
            {
                Id = accion.Id,
                Descripcion = accion.Descripcion,
                Responsable = accion.Responsable,
                FechaCompromiso = accion.FechaCompromiso,
                Completada = accion.Completada,
                FechaCompletada = accion.FechaCompletada
            });
        }

        return detalle;
    }
}
