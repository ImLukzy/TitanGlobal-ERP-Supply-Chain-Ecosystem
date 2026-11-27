using MediatR;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Commands;

public class AnularPrestamoCommand : IRequest<PrestamoIdDto>
{
    public int Id { get; set; }

    public required AnularPrestamoRequestDto Solicitud { get; set; }
}

public class AnularPrestamoCommandHandler : IRequestHandler<AnularPrestamoCommand, PrestamoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public AnularPrestamoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PrestamoIdDto> Handle(AnularPrestamoCommand request, CancellationToken cancellationToken)
    {
        Prestamo prestamo = await ValidacionPrestamo.ObtenerAsync(_unidad, request.Id);
        if (prestamo.Estado != ValidacionPrestamo.EstadoActivo)
        {
            throw ValidacionPrestamo.Error409($"El préstamo {prestamo.Numero} no está activo y no se puede anular.");
        }

        if (prestamo.CuotasPrestamo.Any(cuota => cuota.MontoDescontado > 0m))
        {
            throw ValidacionPrestamo.Error409($"El préstamo {prestamo.Numero} ya tiene descuentos y no se puede anular.");
        }

        string motivo = ValidacionPrestamo.ValidarMotivoAnulacion(request.Solicitud.Motivo);
        prestamo.Estado = ValidacionPrestamo.EstadoAnulado;
        prestamo.Saldo = 0m;
        prestamo.FechaAnulacion = ValidacionPrestamo.Hoy();
        prestamo.MotivoAnulacion = motivo;
        await _unidad.CompleteAsync();
        return new PrestamoIdDto { Id = prestamo.Id };
    }
}
