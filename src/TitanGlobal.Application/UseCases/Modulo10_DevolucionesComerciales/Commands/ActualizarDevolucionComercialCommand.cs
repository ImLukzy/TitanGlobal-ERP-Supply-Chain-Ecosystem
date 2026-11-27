using MediatR;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Commands;

public class ActualizarDevolucionComercialCommand : IRequest<DevolucionCreadaDto>
{
    public int Id { get; set; }

    public required GuardarDevolucionRequestDto Solicitud { get; set; }
}

public class ActualizarDevolucionComercialCommandHandler : IRequestHandler<ActualizarDevolucionComercialCommand, DevolucionCreadaDto>
{
    private readonly IUnitOfWork _unidadTrabajo;

    public ActualizarDevolucionComercialCommandHandler(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<DevolucionCreadaDto> Handle(ActualizarDevolucionComercialCommand request, CancellationToken cancellationToken)
    {
        using (TransactionScope transaccion = OperacionesDevolucionesComerciales.CrearTransaccion())
        {
            GuardarDevolucionRequestDto datos = await ValidacionDevolucion.ValidarAsync(
                request.Solicitud,
                _unidadTrabajo);
            DevolucionComercial devolucion = await OperacionesDevolucionesComerciales.ObtenerEntidadAsync(_unidadTrabajo, request.Id);
            OperacionesDevolucionesComerciales.ExigirEstado(devolucion, "Solicitada");
            List<ProductoDisponibleDto> disponibles = await OperacionesDevolucionesComerciales.CalcularDisponiblesAsync(_unidadTrabajo, datos.OrdenCompraId, request.Id);
            string numeroOrden = await OperacionesDevolucionesComerciales.ObtenerNumeroOrdenAsync(_unidadTrabajo, datos.OrdenCompraId);
            OperacionesDevolucionesComerciales.ValidarCantidadesDisponibles(datos, disponibles, numeroOrden);
            List<DetalleDevolucion> lineasExistentes = await _unidadTrabajo.Repository<DetalleDevolucion>()
                .AsQueryable()
                .Where(detalle => detalle.DevolucionComercialId == request.Id)
                .ToListAsync();
            foreach (DetalleDevolucion lineaExistente in lineasExistentes)
            {
                _unidadTrabajo.Repository<DetalleDevolucion>().Remove(lineaExistente);
            }

            devolucion.ClienteId = datos.ClienteId;
            devolucion.OrdenCompraId = datos.OrdenCompraId;
            devolucion.ReclamoPostventaId = datos.ReclamoPostventaId;
            devolucion.Motivo = datos.Motivo;
            devolucion.Observaciones = datos.Observaciones;
            OperacionesDevolucionesComerciales.AgregarLineas(devolucion, datos.Lineas);
            _unidadTrabajo.Repository<DevolucionComercial>().Update(devolucion);
            await _unidadTrabajo.CompleteAsync();
            transaccion.Complete();
            return OperacionesDevolucionesComerciales.CrearDtoCreado(devolucion);
        }
    }
}
