using MediatR;
using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Commands;

public class CrearDevolucionComercialCommand : IRequest<DevolucionCreadaDto>
{
    public required GuardarDevolucionRequestDto Solicitud { get; set; }
}

public class CrearDevolucionComercialCommandHandler : IRequestHandler<CrearDevolucionComercialCommand, DevolucionCreadaDto>
{
    private readonly IUnitOfWork _unidadTrabajo;

    public CrearDevolucionComercialCommandHandler(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<DevolucionCreadaDto> Handle(CrearDevolucionComercialCommand request, CancellationToken cancellationToken)
    {
        using (TransactionScope transaccion = OperacionesDevolucionesComerciales.CrearTransaccion())
        {
            GuardarDevolucionRequestDto datos = await ValidacionDevolucion.ValidarAsync(
                request.Solicitud,
                _unidadTrabajo);
            List<ProductoDisponibleDto> disponibles = await OperacionesDevolucionesComerciales.CalcularDisponiblesAsync(_unidadTrabajo, datos.OrdenCompraId, null);
            string numeroOrden = await OperacionesDevolucionesComerciales.ObtenerNumeroOrdenAsync(_unidadTrabajo, datos.OrdenCompraId);
            OperacionesDevolucionesComerciales.ValidarCantidadesDisponibles(datos, disponibles, numeroOrden);
            DevolucionComercial devolucion = new DevolucionComercial
            {
                Numero = CrearNumeroTemporal(),
                Fecha = FechasUtc.Normalizar(DateTime.UtcNow),
                ClienteId = datos.ClienteId,
                OrdenCompraId = datos.OrdenCompraId,
                ReclamoPostventaId = datos.ReclamoPostventaId,
                Motivo = datos.Motivo,
                Estado = "Solicitada",
                Observaciones = datos.Observaciones
            };
            try
            {
                await _unidadTrabajo.Repository<DevolucionComercial>().AddAsync(devolucion);
                await _unidadTrabajo.CompleteAsync();
                devolucion.Numero = CrearNumero(devolucion.Id);
                OperacionesDevolucionesComerciales.AgregarLineas(devolucion, datos.Lineas);
                _unidadTrabajo.Repository<DevolucionComercial>().Update(devolucion);
                await _unidadTrabajo.CompleteAsync();
                transaccion.Complete();
            }
            catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status409Conflict,
                    $"Ya existe la devolución {CrearNumero(devolucion.Id)}.");
            }

            return OperacionesDevolucionesComerciales.CrearDtoCreado(devolucion);
        }
    }

    private static string CrearNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    private static string CrearNumero(int id)
    {
        return $"DEV-{id:D6}";
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
