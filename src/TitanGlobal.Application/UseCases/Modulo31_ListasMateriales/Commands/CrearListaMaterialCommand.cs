using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales.Commands;

public class CrearListaMaterialCommand : IRequest<int>
{
    public required CrearListaMaterialRequestDto Solicitud { get; set; }
}

public class CrearListaMaterialCommandHandler : IRequestHandler<CrearListaMaterialCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public CrearListaMaterialCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(CrearListaMaterialCommand request, CancellationToken cancellationToken)
    {
        await ValidacionListaMaterial.ValidarProductoAsync(_unidad, request.Solicitud.ProductoId);
        if (request.Solicitud.Version <= 0)
        {
            throw ValidacionListaMaterial.SolicitudInvalida("La versión debe ser mayor que 0.");
        }

        List<DetalleListaMaterial> detalles = await ValidacionListaMaterial.ValidarComponentesAsync(
            _unidad, request.Solicitud.ProductoId, request.Solicitud.Componentes);
        IGenericRepository<ListaMateriales> repositorio = _unidad.Repository<ListaMateriales>();
        string mensajeDuplicado = $"Ya existe la versión {request.Solicitud.Version} para el producto {request.Solicitud.ProductoId}.";
        if (await repositorio.AsQueryable().AnyAsync(lista =>
                lista.ProductoId == request.Solicitud.ProductoId && lista.Version == request.Solicitud.Version))
        {
            throw ValidacionListaMaterial.Conflicto(mensajeDuplicado);
        }

        ListaMateriales nueva = new ListaMateriales
        {
            ProductoId = request.Solicitud.ProductoId,
            Version = request.Solicitud.Version,
            Estado = ValidacionListaMaterial.Borrador,
            DetallesListaMaterial = detalles
        };
        await repositorio.AddAsync(nueva);

        try
        {
            await PersistenciaListaMaterial.GuardarComponentesAsync(_unidad, request.Solicitud.ProductoId, detalles);
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == PersistenciaListaMaterial.IndiceVersion)
        {
            throw ValidacionListaMaterial.Conflicto(mensajeDuplicado);
        }

        return nueva.Id;
    }
}
