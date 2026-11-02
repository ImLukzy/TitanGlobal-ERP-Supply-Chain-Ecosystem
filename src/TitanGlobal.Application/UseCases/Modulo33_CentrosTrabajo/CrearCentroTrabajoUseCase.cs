using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo;

public class CrearCentroTrabajoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearCentroTrabajoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarCentroTrabajoRequestDto solicitud)
    {
        string codigo = ValidacionCentroTrabajo.NormalizarCodigo(solicitud.Codigo);
        string nombre = ValidacionCentroTrabajo.NormalizarNombre(solicitud.Nombre);
        string tipo = ValidacionCentroTrabajo.NormalizarTipo(solicitud.Tipo);
        string planta = ValidacionCentroTrabajo.NormalizarPlanta(solicitud.Planta);
        decimal capacidadHorasDia = ValidacionCentroTrabajo.ValidarCapacidad(solicitud.CapacidadHorasDia);
        decimal costoHora = ValidacionCentroTrabajo.ValidarCostoHora(solicitud.CostoHora);
        string? responsable = ValidacionCentroTrabajo.NormalizarResponsable(solicitud.Responsable);
        bool codigoExiste = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .AnyAsync(centro => centro.Codigo == codigo);
        if (codigoExiste)
        {
            throw PersistenciaCentroTrabajo.CrearConflictoCodigo(codigo);
        }

        CentroTrabajo centroNuevo = new CentroTrabajo
        {
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Planta = planta,
            CapacidadHorasDia = capacidadHorasDia,
            CostoHora = costoHora,
            Responsable = responsable,
            Activo = solicitud.Activo
        };
        await _unitOfWork.Repository<CentroTrabajo>().AddAsync(centroNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (PersistenciaCentroTrabajo.EsViolacionCodigoUnico(excepcion))
        {
            throw PersistenciaCentroTrabajo.CrearConflictoCodigo(codigo);
        }

        return centroNuevo.Id;
    }
}
