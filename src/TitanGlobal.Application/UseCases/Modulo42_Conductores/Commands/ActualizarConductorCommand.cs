using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores.Commands;

public class ActualizarConductorCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarConductorRequestDto Solicitud { get; set; }
}

public class ActualizarConductorCommandHandler : IRequestHandler<ActualizarConductorCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarConductorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarConductorCommand request, CancellationToken cancellationToken)
    {
        string nombre = ValidacionConductor.NormalizarNombre(request.Solicitud.Nombre);
        string documentoLicencia = ValidacionConductor.NormalizarDocumentoLicencia(request.Solicitud.DocumentoLicencia);

        Conductor? conductor = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .FirstOrDefaultAsync(conductor => conductor.Id == request.Id);

        if (conductor is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El conductor {request.Id} no existe.");
        }

        // Un conductor con licencia vencida se puede editar o desactivar: la regla solo aplica si la fecha cambia.
        DateTime vencimientoLicencia = ValidacionConductor.NormalizarVencimientoLicencia(request.Solicitud.VencimientoLicencia);
        bool vencimientoCambia = vencimientoLicencia.Date != conductor.VencimientoLicencia.Date;
        if (vencimientoCambia)
        {
            ValidacionConductor.ValidarVencimientoLicenciaFutura(vencimientoLicencia);
        }

        string? observaciones = ValidacionConductor.NormalizarObservaciones(request.Solicitud.Observaciones);

        string documentoNormalizado = documentoLicencia.ToLowerInvariant();
        bool documentoEnUso = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .AnyAsync(conductor => conductor.DocumentoLicencia.Trim().ToLower() == documentoNormalizado && conductor.Id != request.Id);

        if (documentoEnUso)
        {
            throw CrearConflictoException(documentoLicencia);
        }

        conductor.Nombre = nombre;
        conductor.DocumentoLicencia = documentoLicencia;
        if (vencimientoCambia)
        {
            conductor.VencimientoLicencia = vencimientoLicencia;
        }

        conductor.Habilitado = request.Solicitud.Habilitado;
        conductor.Activo = request.Solicitud.Activo;
        conductor.Observaciones = observaciones;

        _unitOfWork.Repository<Conductor>().Update(conductor);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoException(documentoLicencia);
        }

        return conductor.Id;
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    private static ReglaNegocioException CrearConflictoException(string documentoLicencia)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un conductor con el documento de licencia {documentoLicencia}.");
    }
}
