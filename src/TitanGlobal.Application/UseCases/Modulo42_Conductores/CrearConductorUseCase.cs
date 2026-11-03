using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores;

public class CrearConductorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearConductorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarConductorRequestDto solicitud)
    {
        string nombre = ValidacionConductor.NormalizarNombre(solicitud.Nombre);
        string documentoLicencia = ValidacionConductor.NormalizarDocumentoLicencia(solicitud.DocumentoLicencia);
        DateTime vencimientoLicencia = ValidacionConductor.NormalizarVencimientoLicencia(solicitud.VencimientoLicencia);
        ValidacionConductor.ValidarVencimientoLicenciaFutura(vencimientoLicencia);
        string? observaciones = ValidacionConductor.NormalizarObservaciones(solicitud.Observaciones);

        bool documentoExiste = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .AnyAsync(conductor => conductor.DocumentoLicencia == documentoLicencia);

        if (documentoExiste)
        {
            throw CrearConflictoException(documentoLicencia);
        }

        Conductor conductorNuevo = new Conductor
        {
            Nombre = nombre,
            DocumentoLicencia = documentoLicencia,
            VencimientoLicencia = vencimientoLicencia,
            Habilitado = solicitud.Habilitado,
            Activo = solicitud.Activo,
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
