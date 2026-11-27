using MediatR;
using TitanGlobal.Application.DTOs.Modulo61_Empleados;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo61_Empleados.Queries;

public class ObtenerEmpleadoQuery : IRequest<EmpleadoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerEmpleadoQueryHandler : IRequestHandler<ObtenerEmpleadoQuery, EmpleadoDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerEmpleadoQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EmpleadoDetalleDto> Handle(ObtenerEmpleadoQuery request, CancellationToken cancellationToken)
    {
        Empleado empleado = await ValidacionEmpleado.ObtenerAsync(_unidad, request.Id);
        return MapearDetalle(empleado);
    }

    private static EmpleadoDetalleDto MapearDetalle(Empleado empleado)
    {
        return new EmpleadoDetalleDto
        {
            Id = empleado.Id,
            Legajo = empleado.Legajo,
            TipoDocumento = empleado.TipoDocumento,
            NumeroDocumento = empleado.NumeroDocumento,
            Nombres = empleado.Nombres,
            Apellidos = empleado.Apellidos,
            Cargo = empleado.Cargo,
            Area = empleado.Area,
            FechaIngreso = empleado.FechaIngreso,
            Estado = empleado.Estado,
            FechaNacimiento = empleado.FechaNacimiento,
            Email = empleado.Email,
            Telefono = empleado.Telefono,
            FechaCese = empleado.FechaCese,
            MotivoCese = empleado.MotivoCese
        };
    }
}
