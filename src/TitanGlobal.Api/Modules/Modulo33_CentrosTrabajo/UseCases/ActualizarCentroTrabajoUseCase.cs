using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.UseCases;

public class ActualizarCentroTrabajoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarCentroTrabajoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarCentroTrabajoRequestDto solicitud)
    {
        CentroTrabajo? centro = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (centro is null)
        {
            throw PersistenciaCentroTrabajo.CrearNoEncontrado(id);
        }

        string codigo = ValidacionCentroTrabajo.NormalizarCodigo(solicitud.Codigo);
        string nombre = ValidacionCentroTrabajo.NormalizarNombre(solicitud.Nombre);
        string tipo = ValidacionCentroTrabajo.NormalizarTipo(solicitud.Tipo);
        string planta = ValidacionCentroTrabajo.NormalizarPlanta(solicitud.Planta);
        decimal capacidadHorasDia = ValidacionCentroTrabajo.ValidarCapacidad(solicitud.CapacidadHorasDia);
        decimal costoHora = ValidacionCentroTrabajo.ValidarCostoHora(solicitud.CostoHora);
        string? responsable = ValidacionCentroTrabajo.NormalizarResponsable(solicitud.Responsable);
        bool codigoExiste = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != id && entidad.Codigo == codigo);
        if (codigoExiste)
        {
            throw PersistenciaCentroTrabajo.CrearConflictoCodigo(codigo);
        }

        centro.Codigo = codigo;
        centro.Nombre = nombre;
        centro.Tipo = tipo;
        centro.Planta = planta;
        centro.CapacidadHorasDia = capacidadHorasDia;
        centro.CostoHora = costoHora;
        centro.Responsable = responsable;
        centro.Activo = solicitud.Activo;
        _unitOfWork.Repository<CentroTrabajo>().Update(centro);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException excepcion) when (PersistenciaCentroTrabajo.EsViolacionCodigoUnico(excepcion))
        {
            throw PersistenciaCentroTrabajo.CrearConflictoCodigo(codigo);
        }

        return centro.Id;
    }
}
