using MediatR;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class RegistrarEntregaCommand : IRequest<EntregaEppIdDto>
{
    public required RegistrarEntregaEppRequestDto Solicitud { get; set; }
}

public class RegistrarEntregaCommandHandler : IRequestHandler<RegistrarEntregaCommand, EntregaEppIdDto>
{
    private readonly IUnitOfWork _unidad;

    public RegistrarEntregaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EntregaEppIdDto> Handle(RegistrarEntregaCommand request, CancellationToken cancellationToken)
    {
        ValidacionSst.ValidarEntrega(request.Solicitud);
        int empleadoId = request.Solicitud.EmpleadoId!.Value;
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, request.Solicitud.EquipoId ?? 0);
        if (!equipo.Activo)
        {
            throw ValidacionSst.EquipoInactivo(equipo.Codigo);
        }

        Empleado empleado = await ValidacionSst.ObtenerEmpleadoAsync(_unidad, empleadoId);
        if (empleado.Estado != ValidacionSst.EmpleadoActivo)
        {
            throw ValidacionSst.EmpleadoCesado(empleadoId);
        }

        DateOnly fechaEntrega = request.Solicitud.FechaEntrega!.Value;
        EntregaEpp entrega = new EntregaEpp
        {
            EquipoProteccionId = equipo.Id,
            EmpleadoId = empleadoId,
            FechaEntrega = fechaEntrega,
            Cantidad = request.Solicitud.Cantidad!.Value,
            FechaVencimiento = fechaEntrega.AddDays(equipo.VidaUtilDias)
        };
        await _unidad.Repository<EntregaEpp>().AddAsync(entrega);
        await _unidad.CompleteAsync();
        return new EntregaEppIdDto { Id = entrega.Id };
    }
}
