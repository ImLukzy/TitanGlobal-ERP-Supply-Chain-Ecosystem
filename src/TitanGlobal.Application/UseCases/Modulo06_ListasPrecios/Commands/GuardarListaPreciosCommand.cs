using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo06_ListasPrecios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo06_ListasPrecios.Commands;

public class GuardarListaPreciosCommand : IRequest<int>
{
    public required GuardarListaRequestDto Solicitud { get; set; }

    public int? Id { get; set; }
}

public class GuardarListaPreciosCommandHandler : IRequestHandler<GuardarListaPreciosCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public GuardarListaPreciosCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GuardarListaPreciosCommand request, CancellationToken cancellationToken)
    {
        ListaPrecios? lista = null;
        if (request.Id.HasValue)
        {
            lista = await _unitOfWork.Repository<ListaPrecios>().AsQueryable()
                .SingleOrDefaultAsync(entidad => entidad.Id == request.Id.Value);
            if (lista is null)
            {
                throw OperacionesListasPrecios.Error(CodigosEstado.Status404NotFound, $"La lista de precios {request.Id.Value} no existe.");
            }
        }

        await ValidacionListaPrecios.ValidarAsync(request.Solicitud, request.Id, _unitOfWork);
        if (lista is null)
        {
            lista = new ListaPrecios();
            await _unitOfWork.Repository<ListaPrecios>().AddAsync(lista);
        }
        else
        {
            List<ItemListaPrecios> existentes = await _unitOfWork.Repository<ItemListaPrecios>().AsQueryable()
                .Where(item => item.ListaPreciosId == lista.Id).ToListAsync();
            foreach (ItemListaPrecios existente in existentes)
            {
                _unitOfWork.Repository<ItemListaPrecios>().Remove(existente);
            }
        }

        lista.Codigo = request.Solicitud.Codigo;
        lista.Nombre = request.Solicitud.Nombre;
        lista.SegmentoClienteId = request.Solicitud.SegmentoClienteId;
        lista.Prioridad = request.Solicitud.Prioridad;
        lista.FechaInicio = FechasUtc.Normalizar(request.Solicitud.FechaInicio);
        lista.FechaFin = request.Solicitud.FechaFin.HasValue ? FechasUtc.Normalizar(request.Solicitud.FechaFin.Value) : null;
        lista.Activa = request.Solicitud.Activa;
        foreach (GuardarItemListaDto item in request.Solicitud.Items)
        {
            lista.Items.Add(new ItemListaPrecios
            {
                ListaPreciosId = lista.Id,
                ProductoId = item.ProductoId,
                Precio = item.Precio
            });
        }

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            if (excepcion.Restriccion?.Contains("Codigo", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw OperacionesListasPrecios.Error(CodigosEstado.Status409Conflict,
                    $"Ya existe una lista de precios con el código {request.Solicitud.Codigo}.");
            }

            throw OperacionesListasPrecios.Error(CodigosEstado.Status409Conflict,
                $"Ya existe una lista de precios con la prioridad {request.Solicitud.Prioridad}.");
        }

        return lista.Id;
    }
}
