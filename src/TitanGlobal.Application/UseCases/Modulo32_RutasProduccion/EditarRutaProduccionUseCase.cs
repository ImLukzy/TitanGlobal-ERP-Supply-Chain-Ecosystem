using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;

public class EditarRutaProduccionUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraRutaProduccion _escritura;

    public EditarRutaProduccionUseCase(IUnitOfWork unidad, IEscrituraRutaProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, EditarRutaProduccionRequestDto solicitud)
    {
        return await _escritura.EjecutarAsync(id, EditarAsync);

        async Task<int> EditarAsync()
        {
            RutaProduccion ruta = await PersistenciaRutaProduccion.ObtenerAsync(_unidad, id);
            if (ruta.Estado != ValidacionRutaProduccion.Borrador)
            {
                throw ValidacionRutaProduccion.Conflicto("Solo se pueden editar rutas de producción en borrador.");
            }

            List<OperacionRutaProduccion> operaciones =
                await ValidacionRutaProduccion.ValidarOperacionesAsync(_unidad, solicitud.Operaciones);
            await ReemplazarOperacionesAsync(id, operaciones);
            await PersistenciaRutaProduccion.GuardarOperacionesAsync(_unidad, ruta.ProductoId, operaciones);
            return id;
        }
    }

    private async Task ReemplazarOperacionesAsync(int id, List<OperacionRutaProduccion> operaciones)
    {
        IGenericRepository<OperacionRutaProduccion> repositorio = _unidad.Repository<OperacionRutaProduccion>();
        List<OperacionRutaProduccion> actuales = await repositorio.AsQueryable()
            .Where(operacion => operacion.RutaProduccionId == id)
            .ToListAsync();
        Dictionary<int, OperacionRutaProduccion> nuevas = operaciones.ToDictionary(operacion => operacion.Secuencia);
        foreach (OperacionRutaProduccion actual in actuales)
        {
            if (nuevas.Remove(actual.Secuencia, out OperacionRutaProduccion? nueva))
            {
                actual.Nombre = nueva.Nombre;
                actual.CentroTrabajoId = nueva.CentroTrabajoId;
                actual.MinutosPreparacion = nueva.MinutosPreparacion;
                actual.MinutosPorUnidad = nueva.MinutosPorUnidad;
            }
            else
            {
                repositorio.Remove(actual);
            }
        }

        foreach (OperacionRutaProduccion nueva in nuevas.Values)
        {
            nueva.RutaProduccionId = id;
            await repositorio.AddAsync(nueva);
        }
    }
}
