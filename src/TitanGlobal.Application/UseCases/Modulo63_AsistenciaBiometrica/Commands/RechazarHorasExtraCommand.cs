using MediatR;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Commands;

public class RechazarHorasExtraCommand : IRequest<JornadaAsistenciaIdDto>
{
    public int Id { get; set; }

    public required RechazarHorasExtraRequestDto Solicitud { get; set; }

    public required string Usuario { get; set; }
}

public class RechazarHorasExtraCommandHandler : IRequestHandler<RechazarHorasExtraCommand, JornadaAsistenciaIdDto>
{
    private readonly IUnitOfWork _unidad;

    public RechazarHorasExtraCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<JornadaAsistenciaIdDto> Handle(RechazarHorasExtraCommand request, CancellationToken cancellationToken)
    {
        JornadaAsistencia jornada = await OperacionesJornadasAsistencia.ObtenerPendienteAsync(_unidad, request.Id);
        string motivo = request.Solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw ValidacionMarcacion.Error400("El motivo del rechazo es obligatorio.");
        }

        if (motivo.Length > MaximoMotivoRechazo)
        {
            throw ValidacionMarcacion.Error400("El motivo del rechazo no puede superar 300 caracteres.");
        }

        jornada.EstadoHorasExtra = CalculadoraJornada.HorasExtraRechazada;
        jornada.MotivoRechazo = motivo;
        OperacionesJornadasAsistencia.Decidir(jornada, request.Usuario);
        await _unidad.CompleteAsync();
        return new JornadaAsistenciaIdDto { Id = jornada.Id };
    }

    private const int MaximoMotivoRechazo = 300;
}
