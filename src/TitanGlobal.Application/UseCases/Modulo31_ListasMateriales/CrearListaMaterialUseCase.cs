using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales;

public class CrearListaMaterialUseCase
{
    private readonly IUnitOfWork _unidad;

    public CrearListaMaterialUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(CrearListaMaterialRequestDto solicitud)
    {
        await ValidacionListaMaterial.ValidarProductoAsync(_unidad, solicitud.ProductoId);
        if (solicitud.Version <= 0)
        {
            throw ValidacionListaMaterial.SolicitudInvalida("La versión debe ser mayor que 0.");
        }

        List<DetalleListaMaterial> detalles = await ValidacionListaMaterial.ValidarComponentesAsync(
            _unidad, solicitud.ProductoId, solicitud.Componentes);
        IGenericRepository<ListaMateriales> repositorio = _unidad.Repository<ListaMateriales>();
        string mensajeDuplicado = $"Ya existe la versión {solicitud.Version} para el producto {solicitud.ProductoId}.";
        if (await repositorio.AsQueryable().AnyAsync(lista =>
                lista.ProductoId == solicitud.ProductoId && lista.Version == solicitud.Version))
        {
            throw ValidacionListaMaterial.Conflicto(mensajeDuplicado);
        }

        ListaMateriales nueva = new ListaMateriales
        {
            ProductoId = solicitud.ProductoId,
            Version = solicitud.Version,
            Estado = ValidacionListaMaterial.Borrador,
            DetallesListaMaterial = detalles
        };
        await repositorio.AddAsync(nueva);

        try
        {
            await PersistenciaListaMaterial.GuardarComponentesAsync(_unidad, solicitud.ProductoId, detalles);
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == PersistenciaListaMaterial.IndiceVersion)
        {
            throw ValidacionListaMaterial.Conflicto(mensajeDuplicado);
        }

        return nueva.Id;
    }
}
