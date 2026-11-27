using MediatR;
using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Commands;

public class CrearReclamoPostventaCommand : IRequest<ReclamoCreadoDto>
{
    public required GuardarReclamoRequestDto Solicitud { get; set; }
}

public class CrearReclamoPostventaCommandHandler : IRequestHandler<CrearReclamoPostventaCommand, ReclamoCreadoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearReclamoPostventaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReclamoCreadoDto> Handle(CrearReclamoPostventaCommand request, CancellationToken cancellationToken)
    {
        GuardarReclamoRequestDto datosValidados = await ValidacionReclamo.ValidarAsync(request.Solicitud, _unitOfWork);
        DateTime fecha = FechasUtc.Normalizar(DateTime.UtcNow);
        ReclamoPostventa reclamo = new ReclamoPostventa
        {
            Numero = CrearNumeroTemporal(),
            Fecha = fecha,
            ClienteId = datosValidados.ClienteId,
            OrdenCompraId = datosValidados.OrdenCompraId,
            ProductoId = datosValidados.ProductoId,
            Tipo = datosValidados.Tipo,
            Prioridad = datosValidados.Prioridad,
            Descripcion = datosValidados.Descripcion,
            Estado = "Abierto",
            FechaLimite = OperacionesReclamosPostventa.CalcularFechaLimite(fecha, datosValidados.Prioridad)
        };

        try
        {
            using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _unitOfWork.Repository<ReclamoPostventa>().AddAsync(reclamo);
                await _unitOfWork.CompleteAsync();
                reclamo.Numero = CrearNumero(reclamo.Id);
                _unitOfWork.Repository<ReclamoPostventa>().Update(reclamo);
                await _unitOfWork.CompleteAsync();
                transaccion.Complete();
            }
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"Ya existe el reclamo {CrearNumero(reclamo.Id)}.");
        }

        return new ReclamoCreadoDto
        {
            Id = reclamo.Id,
            Numero = reclamo.Numero
        };
    }

    private static string CrearNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    private static string CrearNumero(int id)
    {
        return $"REC-{id:D6}";
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
