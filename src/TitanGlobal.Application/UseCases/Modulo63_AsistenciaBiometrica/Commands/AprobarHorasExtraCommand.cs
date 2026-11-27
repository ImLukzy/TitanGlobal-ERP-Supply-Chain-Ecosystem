using MediatR;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Commands;

public class AprobarHorasExtraCommand : IRequest<JornadaAsistenciaIdDto>
{
    public int Id { get; set; }

    public required string Usuario { get; set; }
}

public class AprobarHorasExtraCommandHandler : IRequestHandler<AprobarHorasExtraCommand, JornadaAsistenciaIdDto>
{
    private readonly IUnitOfWork _unidad;

    public AprobarHorasExtraCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<JornadaAsistenciaIdDto> Handle(AprobarHorasExtraCommand request, CancellationToken cancellationToken)
    {
        JornadaAsistencia jornada = await OperacionesJornadasAsistencia.ObtenerPendienteAsync(_unidad, request.Id);
        jornada.EstadoHorasExtra = CalculadoraJornada.HorasExtraAprobada;
        OperacionesJornadasAsistencia.Decidir(jornada, request.Usuario);
        await _unidad.CompleteAsync();
        return new JornadaAsistenciaIdDto { Id = jornada.Id };
    }
}
