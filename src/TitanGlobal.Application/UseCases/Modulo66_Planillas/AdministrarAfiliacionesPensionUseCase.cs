using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas;

public class AdministrarAfiliacionesPensionUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarAfiliacionesPensionUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaAfiliacionesPensionDto> ListarAsync(int? empleadoId, int pagina, int tamanoPagina)
    {
        ValidacionPlanilla.ValidarListado(pagina, tamanoPagina);
        IQueryable<AfiliacionPension> consulta = _unidad.Repository<AfiliacionPension>().AsQueryable().AsNoTracking();
        if (empleadoId is not null)
        {
            await ValidacionPlanilla.ObtenerEmpleadoAsync(_unidad, empleadoId.Value);
            int empleadoFiltro = empleadoId.Value;
            consulta = consulta.Where(afiliacion => afiliacion.EmpleadoId == empleadoFiltro);
        }

        int total = await consulta.CountAsync();
        List<AfiliacionPensionDto> items = await consulta
            .OrderBy(afiliacion => afiliacion.EmpleadoId)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(afiliacion => new AfiliacionPensionDto
            {
                Id = afiliacion.Id,
                EmpleadoId = afiliacion.EmpleadoId,
                EmpleadoLegajo = afiliacion.Empleado.Legajo,
                EmpleadoNombre = afiliacion.Empleado.Apellidos + ", " + afiliacion.Empleado.Nombres,
                CodigoPension = afiliacion.ParametroPension.Codigo,
                Sistema = afiliacion.ParametroPension.Sistema,
                Cuspp = afiliacion.Cuspp,
                FechaAfiliacion = afiliacion.FechaAfiliacion
            })
            .ToListAsync();
        return new PaginaAfiliacionesPensionDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }

    // Crea o reemplaza la afiliación del empleado. Las reglas siguen el orden de la spec: 22, 23, 24, 25 y 17.
    public async Task<AfiliacionPensionIdDto> GuardarAsync(int empleadoId, GuardarAfiliacionPensionRequestDto solicitud)
    {
        string codigo = solicitud.CodigoPension?.Trim() ?? string.Empty;
        if (codigo.Length == 0)
        {
            throw ValidacionPlanilla.Error400("El sistema de pensiones es obligatorio.");
        }

        ParametroPension? parametro = await _unidad.Repository<ParametroPension>().AsQueryable()
            .AsNoTracking()
            .SingleOrDefaultAsync(entidad => entidad.Codigo == codigo);
        if (parametro is null)
        {
            throw ValidacionPlanilla.Error404($"El sistema de pensiones {codigo} no existe.");
        }

        string? cuspp = solicitud.Cuspp?.Trim();
        bool esAfp = parametro.Sistema == ValidacionPlanilla.SistemaAfp;
        if (esAfp && (cuspp is null || cuspp.Length != ValidacionPlanilla.LongitudCuspp))
        {
            throw ValidacionPlanilla.Error400("El CUSPP es obligatorio en una AFP y tiene 12 caracteres.");
        }

        if (solicitud.FechaAfiliacion is null)
        {
            throw ValidacionPlanilla.Error400("La fecha de afiliación es obligatoria.");
        }

        await ValidacionPlanilla.ObtenerEmpleadoAsync(_unidad, empleadoId);
        AfiliacionPension? existente = await _unidad.Repository<AfiliacionPension>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.EmpleadoId == empleadoId);
        string? cusppGuardado = esAfp ? cuspp : null;
        if (existente is null)
        {
            existente = new AfiliacionPension
            {
                EmpleadoId = empleadoId,
                ParametroPensionId = parametro.Id,
                Cuspp = cusppGuardado,
                FechaAfiliacion = solicitud.FechaAfiliacion.Value
            };
            await _unidad.Repository<AfiliacionPension>().AddAsync(existente);
        }
        else
        {
            existente.ParametroPensionId = parametro.Id;
            existente.Cuspp = cusppGuardado;
            existente.FechaAfiliacion = solicitud.FechaAfiliacion.Value;
        }

        await _unidad.CompleteAsync();
        return new AfiliacionPensionIdDto { Id = existente.Id };
    }
}
