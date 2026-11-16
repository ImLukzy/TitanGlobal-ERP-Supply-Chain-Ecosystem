using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class CrearLicitacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearLicitacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(CrearLicitacionRequestDto solicitud)
    {
        string numero = ValidacionLicitacion.NormalizarNumero(solicitud.Numero);
        string titulo = ValidacionLicitacion.NormalizarTitulo(solicitud.Titulo);
        string tipo = ValidacionLicitacion.NormalizarTipo(solicitud.Tipo);
        ValidacionLicitacion.ValidarPresupuesto(solicitud.PresupuestoReferencial);
        DateTime fechaCierre = ValidacionLicitacion.ValidarFechaCierre(solicitud.FechaCierre);
        string? descripcion = ValidacionLicitacion.NormalizarDescripcion(solicitud.Descripcion);

        bool numeroExiste = await _unitOfWork.Repository<Licitacion>()
            .AsQueryable()
            .AnyAsync(licitacion => licitacion.Numero == numero);
        if (numeroExiste)
        {
            throw CrearConflictoNumero(numero);
        }

        Licitacion licitacionNueva = new Licitacion
        {
            Numero = numero,
            Titulo = titulo,
            Descripcion = descripcion,
            Tipo = tipo,
            Estado = ValidacionLicitacion.Abierta,
            PresupuestoReferencial = solicitud.PresupuestoReferencial,
            FechaCierre = fechaCierre,
            FechaRegistro = DateTime.UtcNow
        };
        await _unitOfWork.Repository<Licitacion>().AddAsync(licitacionNueva);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoNumero(numero);
        }

        return licitacionNueva.Id;
    }

    private static ReglaNegocioException CrearConflictoNumero(string numero)
    {
        return ValidacionLicitacion.Conflicto($"Ya existe una licitación con el número {numero}.");
    }
}
