using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

public class CrearOrdenProduccionUseCase
{
    private const string Activa = "Activa";
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraOrdenProduccion _escritura;

    public CrearOrdenProduccionUseCase(IUnitOfWork unidad, IEscrituraOrdenProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<OrdenProduccionCreadaDto> ExecuteAsync(CrearOrdenProduccionRequestDto solicitud)
    {
        int productoId = solicitud.ProductoId;
        ValidacionOrdenProduccion.ValidarIdProducto(productoId);
        int cantidad = ValidacionOrdenProduccion.ValidarCantidadPlanificada(solicitud.CantidadPlanificada);
        (DateOnly inicio, DateOnly fin) = ValidacionOrdenProduccion.ValidarFechasPlanificadas(
            solicitud.FechaInicioPlanificada, solicitud.FechaFinPlanificada);

        bool productoExiste = await _unidad.Repository<Producto>().AsQueryable()
            .AnyAsync(producto => producto.Id == productoId);
        if (!productoExiste)
        {
            throw ValidacionOrdenProduccion.ProductoInexistente(productoId);
        }

        // PATRÓN LINQ 5: se toma la versión activa en el momento del alta.
        int? listaId = await _unidad.Repository<ListaMateriales>().AsQueryable()
            .Where(lista => lista.ProductoId == productoId && lista.Estado == Activa)
            .OrderByDescending(lista => lista.Version)
            .Select(lista => (int?)lista.Id)
            .FirstOrDefaultAsync();
        if (!listaId.HasValue)
        {
            throw ValidacionOrdenProduccion.Conflicto(
                $"El producto {productoId} no tiene una lista de materiales activa.");
        }

        int? rutaId = await _unidad.Repository<RutaProduccion>().AsQueryable()
            .Where(ruta => ruta.ProductoId == productoId && ruta.Estado == Activa)
            .OrderByDescending(ruta => ruta.Version)
            .Select(ruta => (int?)ruta.Id)
            .FirstOrDefaultAsync();
        if (!rutaId.HasValue)
        {
            throw ValidacionOrdenProduccion.Conflicto(
                $"El producto {productoId} no tiene una ruta de producción activa.");
        }

        OrdenProduccion orden = new OrdenProduccion
        {
            Numero = ValidacionOrdenProduccion.NumeroTemporal(),
            ProductoId = productoId,
            ListaMaterialesId = listaId.Value,
            RutaProduccionId = rutaId.Value,
            CantidadPlanificada = cantidad,
            FechaInicioPlanificada = inicio,
            FechaFinPlanificada = fin,
            Estado = ValidacionOrdenProduccion.Planificada
        };

        int id = await _escritura.EjecutarAsync(GuardarAsync);
        return new OrdenProduccionCreadaDto(id, orden.Numero);

        async Task<int> GuardarAsync()
        {
            await _unidad.Repository<OrdenProduccion>().AddAsync(orden);
            try
            {
                await _unidad.CompleteAsync();
            }
            catch (ViolacionRestriccionException error) when (
                error.Tipo == TipoViolacionRestriccion.ReferenciaForanea
                && error.Restriccion == ValidacionOrdenProduccion.ReferenciaProducto)
            {
                throw ValidacionOrdenProduccion.ProductoInexistente(productoId);
            }

            // El número definitivo sale del Id, como CTR-000001 en el módulo 62.
            orden.Numero = ValidacionOrdenProduccion.NumeroDefinitivo(orden.Id);
            await _unidad.CompleteAsync();
            return orden.Id;
        }
    }
}
