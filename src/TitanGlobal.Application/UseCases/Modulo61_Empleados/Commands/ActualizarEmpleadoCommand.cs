using MediatR;
using TitanGlobal.Application.DTOs.Modulo61_Empleados;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo61_Empleados.Commands;

public class ActualizarEmpleadoCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarEmpleadoRequestDto Solicitud { get; set; }
}

public class ActualizarEmpleadoCommandHandler : IRequestHandler<ActualizarEmpleadoCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public ActualizarEmpleadoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(ActualizarEmpleadoCommand request, CancellationToken cancellationToken)
    {
        ValidacionEmpleado.DatosEmpleado datos = ValidacionEmpleado.Normalizar(request.Solicitud);
        ValidacionEmpleado.ValidarEdadMinima(datos.FechaNacimiento, datos.FechaIngreso);
        await ValidacionEmpleado.ValidarDocumentoUnicoAsync(_unidad, datos.TipoDocumento, datos.NumeroDocumento, request.Id);
        Empleado empleado = await ValidacionEmpleado.ObtenerAsync(_unidad, request.Id);
        ValidacionEmpleado.ValidarEditable(empleado);
        OperacionesEmpleados.AplicarDatos(empleado, datos);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEmpleado.EsDuplicadoDocumento(excepcion))
        {
            throw ValidacionEmpleado.CrearConflictoDocumento(datos.TipoDocumento, datos.NumeroDocumento);
        }

        return empleado.Id;
    }
}
