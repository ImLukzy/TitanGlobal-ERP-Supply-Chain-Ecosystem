using MediatR;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Commands;

public class ActualizarReclamoPostventaCommand : IRequest<ReclamoCreadoDto>
{
    public int Id { get; set; }

    public required GuardarReclamoRequestDto Solicitud { get; set; }
}

public class ActualizarReclamoPostventaCommandHandler : IRequestHandler<ActualizarReclamoPostventaCommand, ReclamoCreadoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarReclamoPostventaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReclamoCreadoDto> Handle(ActualizarReclamoPostventaCommand request, CancellationToken cancellationToken)
    {
        GuardarReclamoRequestDto datosValidados = await ValidacionReclamo.ValidarAsync(request.Solicitud, _unitOfWork);
        ReclamoPostventa reclamo = await OperacionesReclamosPostventa.ObtenerEntidadAsync(_unitOfWork, request.Id);
        OperacionesReclamosPostventa.ExigirEstado(reclamo, "Abierto");
        reclamo.ClienteId = datosValidados.ClienteId;
        reclamo.OrdenCompraId = datosValidados.OrdenCompraId;
        reclamo.ProductoId = datosValidados.ProductoId;
        reclamo.Tipo = datosValidados.Tipo;
        reclamo.Prioridad = datosValidados.Prioridad;
        reclamo.Descripcion = datosValidados.Descripcion;
        reclamo.FechaLimite = OperacionesReclamosPostventa.CalcularFechaLimite(reclamo.Fecha, reclamo.Prioridad);
        _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
        await _unitOfWork.CompleteAsync();
        return CrearDtoCreado(reclamo);
    }

    private static ReclamoCreadoDto CrearDtoCreado(ReclamoPostventa reclamo)
    {
        return new ReclamoCreadoDto { Id = reclamo.Id, Numero = reclamo.Numero };
    }
}
