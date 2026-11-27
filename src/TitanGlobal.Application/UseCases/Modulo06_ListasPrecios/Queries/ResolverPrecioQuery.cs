using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;
using TitanGlobal.Application.DTOs.Modulo06_ListasPrecios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo06_ListasPrecios.Queries;

public class ResolverPrecioQuery : IRequest<PrecioResueltoDto>
{
    public int ProductoId { get; set; }

    public int ClienteId { get; set; }

    public DateTime? Fecha { get; set; }
}

public class ResolverPrecioQueryHandler : IRequestHandler<ResolverPrecioQuery, PrecioResueltoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResolverPrecioQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PrecioResueltoDto> Handle(ResolverPrecioQuery request, CancellationToken cancellationToken)
    {
        Producto? producto = await _unitOfWork.Repository<Producto>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.ProductoId);
        if (producto is null)
        {
            throw OperacionesListasPrecios.Error(CodigosEstado.Status400BadRequest, $"El producto {request.ProductoId} no existe.");
        }

        ClienteClasificadoDto? clienteClasificado = await ConsultaSegmentacionClientes.ObtenerClienteClasificadoAsync(request.ClienteId, _unitOfWork);
        if (clienteClasificado is null)
        {
            throw OperacionesListasPrecios.Error(CodigosEstado.Status400BadRequest, $"El cliente {request.ClienteId} no existe.");
        }

        DateTime fechaUtc = FechasUtc.Normalizar(request.Fecha ?? DateTime.UtcNow).Date;
        List<CandidatoPrecio> candidatos = await _unitOfWork.Repository<ItemListaPrecios>().AsQueryable()
            .Where(item => item.ProductoId == request.ProductoId)
            .Join(_unitOfWork.Repository<ListaPrecios>().AsQueryable(), item => item.ListaPreciosId, lista => lista.Id,
                (item, lista) => new CandidatoPrecio
                {
                    ListaId = lista.Id,
                    Codigo = lista.Codigo,
                    SegmentoId = lista.SegmentoClienteId,
                    Prioridad = lista.Prioridad,
                    Activa = lista.Activa,
                    FechaInicio = lista.FechaInicio,
                    FechaFin = lista.FechaFin,
                    Precio = item.Precio
                })
            .Where(candidato => candidato.Activa && candidato.FechaInicio <= fechaUtc
                && (!candidato.FechaFin.HasValue || candidato.FechaFin.Value >= fechaUtc)
                && (!candidato.SegmentoId.HasValue || candidato.SegmentoId == clienteClasificado.SegmentoId))
            .OrderByDescending(candidato => candidato.SegmentoId.HasValue)
            .ThenBy(candidato => candidato.Prioridad).ToListAsync();
        CandidatoPrecio? elegido = candidatos.FirstOrDefault();
        return new PrecioResueltoDto
        {
            ProductoId = producto.Id,
            ProductoNombre = producto.Nombre,
            ClienteId = request.ClienteId,
            ClienteNombre = clienteClasificado.Nombre,
            SegmentoCodigo = clienteClasificado.SegmentoCodigo,
            Fecha = fechaUtc,
            PrecioBase = producto.Precio,
            Precio = elegido?.Precio ?? producto.Precio,
            Origen = elegido is null ? "Base" : elegido.SegmentoId.HasValue ? "Segmento" : "General",
            ListaId = elegido?.ListaId,
            ListaCodigo = elegido?.Codigo
        };
    }

    private sealed class CandidatoPrecio
    {
        public int ListaId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int? SegmentoId { get; set; }
        public int Prioridad { get; set; }
        public bool Activa { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal Precio { get; set; }
    }
}
