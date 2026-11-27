using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Commands;

public class RegistrarMarcacionCommand : IRequest<MarcacionIdDto>
{
    public required RegistrarMarcacionRequestDto Solicitud { get; set; }
}

public class RegistrarMarcacionCommandHandler : IRequestHandler<RegistrarMarcacionCommand, MarcacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public RegistrarMarcacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<MarcacionIdDto> Handle(RegistrarMarcacionCommand request, CancellationToken cancellationToken)
    {
        if (request.Solicitud.EmpleadoId is null)
        {
            throw ValidacionMarcacion.Error400("El empleado es obligatorio.");
        }

        string tipo = request.Solicitud.Tipo?.Trim() ?? string.Empty;
        if (!ValidacionMarcacion.EsTipoValido(tipo))
        {
            throw ValidacionMarcacion.Error400("El tipo de marcación debe ser Entrada o Salida.");
        }

        if (request.Solicitud.Instante is null)
        {
            throw ValidacionMarcacion.Error400("El instante de la marcación es obligatorio.");
        }

        string observacion = request.Solicitud.Observacion?.Trim() ?? string.Empty;
        if (observacion.Length == 0)
        {
            throw ValidacionMarcacion.Error400("La observación es obligatoria en una marcación manual.");
        }

        if (observacion.Length > MaximoObservacion)
        {
            throw ValidacionMarcacion.Error400("La observación no puede superar 200 caracteres.");
        }

        int empleadoId = request.Solicitud.EmpleadoId.Value;
        Empleado? empleado = await _unidad.Repository<Empleado>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == empleadoId);
        if (empleado is null)
        {
            throw ValidacionMarcacion.EmpleadoInexistente(empleadoId);
        }

        DateTime instante = FechasUtc.Normalizar(request.Solicitud.Instante.Value);
        DateOnly fechaLocal = CalculadoraJornada.FechaLocal(instante);
        List<ContratoLaboral> contratos = await _unidad.Repository<ContratoLaboral>().AsQueryable()
            .Where(contrato => contrato.EmpleadoId == empleadoId)
            .ToListAsync();
        string? motivo = ValidacionMarcacion.MotivoNoVigente(empleado, contratos, fechaLocal);
        if (motivo is not null)
        {
            throw ValidacionMarcacion.Error409(motivo);
        }

        DateOnly? fechaDecidida = await ValidacionMarcacion.BuscarJornadaDecididaAsync(_unidad, empleadoId, instante);
        if (fechaDecidida is not null)
        {
            throw ValidacionMarcacion.JornadaDecidida(fechaDecidida.Value);
        }

        DateTime desdeDuplicado = instante.AddSeconds(-ValidacionMarcacion.SegundosDuplicado);
        DateTime hastaDuplicado = instante.AddSeconds(ValidacionMarcacion.SegundosDuplicado);
        bool duplicada = await _unidad.Repository<Marcacion>().AsQueryable()
            .AnyAsync(marcacion => marcacion.EmpleadoId == empleadoId
                && marcacion.Tipo == tipo
                && marcacion.Instante >= desdeDuplicado
                && marcacion.Instante <= hastaDuplicado);
        if (duplicada)
        {
            throw ValidacionMarcacion.Error409(ValidacionMarcacion.MarcacionDuplicada());
        }

        Marcacion marcacionNueva = new Marcacion
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Instante = instante,
            Origen = CalculadoraJornada.OrigenManual,
            Observacion = observacion
        };
        await _unidad.Repository<Marcacion>().AddAsync(marcacionNueva);
        await _unidad.CompleteAsync();
        return new MarcacionIdDto { Id = marcacionNueva.Id };
    }

    private const int MaximoObservacion = 200;
}
