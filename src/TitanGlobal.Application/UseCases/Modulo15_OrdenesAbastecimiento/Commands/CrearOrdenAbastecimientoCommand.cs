using MediatR;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento.Commands;

public class CrearOrdenAbastecimientoCommand : IRequest<OrdenAbastecimientoCreadaDto>
{
    public required GuardarOrdenAbastecimientoRequestDto Solicitud { get; set; }
}

public class CrearOrdenAbastecimientoCommandHandler : IRequestHandler<CrearOrdenAbastecimientoCommand, OrdenAbastecimientoCreadaDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearOrdenAbastecimientoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoCreadaDto> Handle(CrearOrdenAbastecimientoCommand request, CancellationToken cancellationToken)
    {
        GuardarOrdenAbastecimientoRequestDto datosValidados = await ValidacionOrdenAbastecimiento.ValidarAsync(
            request.Solicitud,
            _unitOfWork);
        OrdenAbastecimiento orden = new OrdenAbastecimiento
        {
            Fecha = FechasUtc.Normalizar(DateTime.UtcNow),
            ProveedorId = datosValidados.ProveedorId,
            Estado = "Emitida",
            FechaEntregaEstimada = datosValidados.FechaEntregaEstimada,
            Observaciones = datosValidados.Observaciones
        };
        foreach (LineaOrdenAbastecimientoRequestDto linea in datosValidados.Lineas!)
        {
            orden.Detalles.Add(new DetalleOrdenAbastecimiento
            {
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario
            });
        }

        return await PersistenciaOrdenAbastecimiento.CrearAsync(orden, _unitOfWork);
    }
}
