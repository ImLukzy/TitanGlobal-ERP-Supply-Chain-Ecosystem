using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores;

public class ActualizarConductorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarConductorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarConductorRequestDto solicitud)
    {
        string nombre = ValidacionConductor.NormalizarNombre(solicitud.Nombre);
        string documentoLicencia = ValidacionConductor.NormalizarDocumentoLicencia(solicitud.DocumentoLicencia);

        Conductor? conductor = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .FirstOrDefaultAsync(conductor => conductor.Id == id);

        if (conductor is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El conductor {id} no existe.");
        }

        // Un conductor con licencia vencida se puede editar o desactivar: la regla solo aplica si la fecha cambia.
        DateTime vencimientoLicencia = ValidacionConductor.NormalizarVencimientoLicencia(solicitud.VencimientoLicencia);
        bool vencimientoCambia = vencimientoLicencia.Date != conductor.VencimientoLicencia.Date;
        if (vencimientoCambia)
        {
            ValidacionConductor.ValidarVencimientoLicenciaFutura(vencimientoLicencia);
        }

        string? observaciones = ValidacionConductor.NormalizarObservaciones(solicitud.Observaciones);

        bool documentoEnUso = await _unitOfWork.Repository<Conductor>()
            .AsQueryable()
            .AnyAsync(conductor => conductor.DocumentoLicencia == documentoLicencia && conductor.Id != id);

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

        conductor.Habilitado = solicitud.Habilitado;
        conductor.Activo = solicitud.Activo;
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
