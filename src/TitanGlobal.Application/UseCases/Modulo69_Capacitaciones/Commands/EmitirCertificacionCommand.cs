using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class EmitirCertificacionCommand : IRequest<CertificacionIdDto>
{
    public int Id { get; set; }

    public int InscripcionId { get; set; }

    public required EmitirCertificacionRequestDto Solicitud { get; set; }
}

public class EmitirCertificacionCommandHandler : IRequestHandler<EmitirCertificacionCommand, CertificacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public EmitirCertificacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CertificacionIdDto> Handle(EmitirCertificacionCommand request, CancellationToken cancellationToken)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, request.Id);
        InscripcionCurso inscripcion = await OperacionesProgramacionesCurso.ObtenerInscripcionAsync(_unidad, request.Id, request.InscripcionId);
        if (inscripcion.Estado != ValidacionCapacitacion.InscripcionAprobado)
        {
            throw ValidacionCapacitacion.Error409($"La inscripción {request.InscripcionId} no está aprobada y no admite certificación.");
        }

        bool tieneCertificacion = await _unidad.Repository<CertificacionCurso>().AsQueryable()
            .AnyAsync(certificacion => certificacion.InscripcionCursoId == request.InscripcionId);
        if (tieneCertificacion)
        {
            throw ValidacionCapacitacion.CertificacionDuplicada(request.InscripcionId);
        }

        string codigo = request.Solicitud.Codigo?.Trim() ?? string.Empty;
        if (codigo.Length == 0 || codigo.Length > ValidacionCapacitacion.MaximoCodigoCertificacion)
        {
            throw ValidacionCapacitacion.Error400("El código de certificación es obligatorio y no puede superar 40 caracteres.");
        }

        if (request.Solicitud.FechaEmision is null || request.Solicitud.FechaEmision.Value < programacion.FechaFin)
        {
            throw ValidacionCapacitacion.Error400("La fecha de emisión es obligatoria y no puede ser anterior al fin del curso.");
        }

        DateOnly emision = request.Solicitud.FechaEmision.Value;
        if (request.Solicitud.FechaVencimiento is not null && request.Solicitud.FechaVencimiento.Value < emision)
        {
            throw ValidacionCapacitacion.Error400("La fecha de vencimiento no puede ser anterior a la de emisión.");
        }

        codigo = codigo.ToUpperInvariant();
        bool codigoExiste = await _unidad.Repository<CertificacionCurso>().AsQueryable()
            .AnyAsync(certificacion => certificacion.Codigo == codigo);
        if (codigoExiste)
        {
            throw ValidacionCapacitacion.CodigoCertificacionDuplicado(codigo);
        }

        CertificacionCurso nueva = new CertificacionCurso
        {
            InscripcionCursoId = request.InscripcionId,
            Codigo = codigo,
            FechaEmision = emision,
            FechaVencimiento = request.Solicitud.FechaVencimiento
        };
        try
        {
            await _unidad.Repository<CertificacionCurso>().AddAsync(nueva);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionCapacitacion.EsCertificacionDuplicada(excepcion))
        {
            throw ValidacionCapacitacion.CertificacionDuplicada(request.InscripcionId);
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionCapacitacion.EsCodigoCertificacionDuplicado(excepcion))
        {
            throw ValidacionCapacitacion.CodigoCertificacionDuplicado(codigo);
        }

        return new CertificacionIdDto { Id = nueva.Id };
    }
}
