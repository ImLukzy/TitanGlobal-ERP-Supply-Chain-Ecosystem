using MediatR;
using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo61_Empleados;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo61_Empleados.Commands;

public class CrearEmpleadoCommand : IRequest<int>
{
    public required GuardarEmpleadoRequestDto Solicitud { get; set; }
}

public class CrearEmpleadoCommandHandler : IRequestHandler<CrearEmpleadoCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public CrearEmpleadoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(CrearEmpleadoCommand request, CancellationToken cancellationToken)
    {
        ValidacionEmpleado.DatosEmpleado datos = ValidacionEmpleado.Normalizar(request.Solicitud);
        ValidacionEmpleado.ValidarEdadMinima(datos.FechaNacimiento, datos.FechaIngreso);
        await ValidacionEmpleado.ValidarDocumentoUnicoAsync(_unidad, datos.TipoDocumento, datos.NumeroDocumento, null);
        Empleado empleado = new Empleado
        {
            Legajo = $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}",
            Estado = ValidacionEmpleado.EstadoActivo
        };
        OperacionesEmpleados.AplicarDatos(empleado, datos);
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            await _unidad.Repository<Empleado>().AddAsync(empleado);
            await _unidad.CompleteAsync();
            empleado.Legajo = $"EMP-{empleado.Id:D6}";
            _unidad.Repository<Empleado>().Update(empleado);
            await _unidad.CompleteAsync();
            transaccion.Complete();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEmpleado.EsDuplicadoDocumento(excepcion))
        {
            throw ValidacionEmpleado.CrearConflictoDocumento(datos.TipoDocumento, datos.NumeroDocumento);
        }

        return empleado.Id;
    }
}
