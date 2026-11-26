using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas;

public class AdministrarBonosPlanillaUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarBonosPlanillaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaBonosPlanillaDto> ListarAsync(int? anio, int? mes, int? empleadoId, int pagina, int tamanoPagina)
    {
        ValidacionPlanilla.ValidarListado(pagina, tamanoPagina);
        IQueryable<BonoPlanilla> consulta = _unidad.Repository<BonoPlanilla>().AsQueryable().AsNoTracking();
        if (anio is not null)
        {
            int anioFiltro = anio.Value;
            consulta = consulta.Where(bono => bono.Anio == anioFiltro);
        }

        if (mes is not null)
        {
            int mesFiltro = mes.Value;
            consulta = consulta.Where(bono => bono.Mes == mesFiltro);
        }

        if (empleadoId is not null)
        {
            int empleadoFiltro = empleadoId.Value;
            consulta = consulta.Where(bono => bono.EmpleadoId == empleadoFiltro);
        }

        int total = await consulta.CountAsync();
        List<BonoPlanillaDto> items = await consulta
            .OrderByDescending(bono => bono.Anio)
            .ThenByDescending(bono => bono.Mes)
            .ThenBy(bono => bono.EmpleadoId)
            .ThenBy(bono => bono.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(bono => new BonoPlanillaDto
            {
                Id = bono.Id,
                EmpleadoId = bono.EmpleadoId,
                EmpleadoLegajo = bono.Empleado.Legajo,
                EmpleadoNombre = bono.Empleado.Apellidos + ", " + bono.Empleado.Nombres,
                Anio = bono.Anio,
                Mes = bono.Mes,
                TipoPlanilla = bono.TipoPlanilla,
                Concepto = bono.Concepto,
                Monto = bono.Monto,
                PlanillaId = bono.PlanillaId,
                PlanillaNumero = bono.Planilla == null ? null : bono.Planilla.Numero
            })
            .ToListAsync();
        return new PaginaBonosPlanillaDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }

    public async Task<BonoPlanillaIdDto> CrearAsync(GuardarBonoPlanillaRequestDto solicitud)
    {
        if (solicitud.EmpleadoId is null)
        {
            throw ValidacionPlanilla.Error400("El empleado es obligatorio.");
        }

        int anio = ValidacionPlanilla.ValidarAnio(solicitud.Anio);
        int mes = ValidacionPlanilla.ValidarMes(solicitud.Mes);
        string tipo = ValidacionPlanilla.ValidarTipo(solicitud.TipoPlanilla);
        string concepto = ValidacionPlanilla.ValidarConcepto(solicitud.Concepto);
        decimal monto = ValidacionPlanilla.ValidarMonto(solicitud.Monto);
        await ValidacionPlanilla.ObtenerEmpleadoAsync(_unidad, solicitud.EmpleadoId.Value);
        BonoPlanilla bono = new BonoPlanilla
        {
            EmpleadoId = solicitud.EmpleadoId.Value,
            Anio = anio,
            Mes = mes,
            TipoPlanilla = tipo,
            Concepto = concepto,
            Monto = monto
        };
        await _unidad.Repository<BonoPlanilla>().AddAsync(bono);
        await _unidad.CompleteAsync();
        return new BonoPlanillaIdDto { Id = bono.Id };
    }

    public async Task EliminarAsync(int id)
    {
        BonoPlanilla? bono = await _unidad.Repository<BonoPlanilla>().AsQueryable()
            .Include(entidad => entidad.Planilla)
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (bono is null)
        {
            throw ValidacionPlanilla.Error404($"El bono {id} no existe.");
        }

        if (bono.Planilla is not null && ValidacionPlanilla.EstaCerrada(bono.Planilla.Estado))
        {
            throw ValidacionPlanilla.Error409("El bono está en una planilla aprobada y no se puede eliminar.");
        }

        _unidad.Repository<BonoPlanilla>().Remove(bono);
        await _unidad.CompleteAsync();
    }
}
