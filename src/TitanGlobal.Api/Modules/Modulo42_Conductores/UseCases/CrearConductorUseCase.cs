using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo42_Conductores.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo42_Conductores.UseCases;

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
        DateTime vencimientoLicencia = ValidacionConductor.ValidarVencimientoLicencia(solicitud.VencimientoLicencia);

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
            Observaciones = solicitud.Observaciones
        };

        await _unitOfWork.Repository<Conductor>().AddAsync(conductorNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoException(documentoLicencia);
        }

        return conductorNuevo.Id;
    }

    private static bool EsViolacionUnica(DbUpdateException excepcion)
    {
        return excepcion.InnerException is PostgresException excepcionPostgres
            && excepcionPostgres.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static ReglaNegocioException CrearConflictoException(string documentoLicencia)
    {
        return new ReglaNegocioException(
            StatusCodes.Status409Conflict,
            $"Ya existe un conductor con el documento de licencia {documentoLicencia}.");
    }
}
