using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores.Commands;

public class CrearConductorCommand : IRequest<int>
{
    public required GuardarConductorRequestDto Solicitud { get; set; }
}

public class CrearConductorCommandHandler : IRequestHandler<CrearConductorCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearConductorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearConductorCommand request, CancellationToken cancellationToken)
    {
        string nombre = ValidacionConductor.NormalizarNombre(request.Solicitud.Nombre);
        string documentoLicencia = ValidacionConductor.NormalizarDocumentoLicencia(request.Solicitud.DocumentoLicencia);
        DateTime vencimientoLicencia = ValidacionConductor.NormalizarVencimientoLicencia(request.Solicitud.VencimientoLicencia);
        ValidacionConductor.ValidarVencimientoLicenciaFutura(vencimientoLicencia);
        string? observaciones = ValidacionConductor.NormalizarObservaciones(request.Solicitud.Observaciones);

        string documentoNormalizado = documentoLicencia.ToLowerInvariant();
        bool documentoExiste = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .AnyAsync(conductor => conductor.DocumentoLicencia.Trim().ToLower() == documentoNormalizado);

        if (documentoExiste)
        {
            throw CrearConflictoException(documentoLicencia);
        }

        Conductor conductorNuevo = new Conductor
        {
            Nombre = nombre,
            DocumentoLicencia = documentoLicencia,
            VencimientoLicencia = vencimientoLicencia,
            Habilitado = request.Solicitud.Habilitado,
            Activo = request.Solicitud.Activo,
            Observaciones = observaciones
        };

        await _unitOfWork.Repository<Conductor>().AddAsync(conductorNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoException(documentoLicencia);
        }

        return conductorNuevo.Id;
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
