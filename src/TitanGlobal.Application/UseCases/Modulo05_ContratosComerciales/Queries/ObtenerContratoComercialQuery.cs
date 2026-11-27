using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Queries;

public class ObtenerContratoComercialQuery : IRequest<ContratoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerContratoComercialQueryHandler : IRequestHandler<ObtenerContratoComercialQuery, ContratoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerContratoComercialQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ContratoDetalleDto> Handle(ObtenerContratoComercialQuery request, CancellationToken cancellationToken)
    {
        ContratoComercial contrato = await OperacionesContratosComerciales.ObtenerEntidadAsync(_unitOfWork, request.Id);
        ContratoProyeccionDto proyeccion = await ProyectarAsync(contrato);
        List<OrdenPeriodoDto> ordenes = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => orden.ClienteId == contrato.ClienteId
                && orden.Fecha >= contrato.FechaInicio
                && orden.Fecha < contrato.FechaFin.AddDays(1))
            .OrderByDescending(orden => orden.Fecha)
            .Select(orden => new OrdenPeriodoDto
            {
                OrdenId = orden.Id,
                NumeroOrden = orden.NumeroOrden,
                Fecha = orden.Fecha,
                Total = orden.Total
            })
            .ToListAsync();
        return MapearDetalle(proyeccion, ordenes);
    }

    private async Task<ContratoProyeccionDto> ProyectarAsync(ContratoComercial contrato)
    {
        return await _unitOfWork.Repository<ContratoComercial>().AsQueryable()
            .Where(entidad => entidad.Id == contrato.Id)
            .Select(entidad => new ContratoProyeccionDto
            {
                Id = entidad.Id,
                Numero = entidad.Numero,
                ClienteId = entidad.ClienteId,
                ClienteNombre = entidad.Cliente.Nombre,
                Titulo = entidad.Titulo,
                FechaInicio = entidad.FechaInicio,
                FechaFin = entidad.FechaFin,
                Estado = entidad.Estado,
                MontoComprometido = entidad.MontoComprometido,
                CondicionPago = entidad.CondicionPago,
                Observaciones = entidad.Observaciones,
                MotivoCancelacion = entidad.MotivoCancelacion,
                FechaActivacion = entidad.FechaActivacion,
                FechaCierre = entidad.FechaCierre,
                MontoConsumido = _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                    .Where(orden => orden.ClienteId == entidad.ClienteId
                        && orden.Fecha >= entidad.FechaInicio && orden.Fecha < entidad.FechaFin.AddDays(1))
                    .Sum(orden => (decimal?)orden.Total) ?? 0m
            })
            .SingleAsync();
    }

    private static ContratoDetalleDto MapearDetalle(ContratoProyeccionDto proyeccion, List<OrdenPeriodoDto> ordenes)
    {
        ContratoListadoDto listado = OperacionesContratosComerciales.MapearListado(proyeccion);
        return new ContratoDetalleDto
        {
            Id = listado.Id,
            Numero = listado.Numero,
            ClienteId = listado.ClienteId,
            ClienteNombre = listado.ClienteNombre,
            Titulo = listado.Titulo,
            FechaInicio = listado.FechaInicio,
            FechaFin = listado.FechaFin,
            Estado = listado.Estado,
            Vencido = listado.Vencido,
            MontoComprometido = listado.MontoComprometido,
            MontoConsumido = listado.MontoConsumido,
            PorcentajeConsumido = listado.PorcentajeConsumido,
            CondicionPago = proyeccion.CondicionPago,
            Observaciones = proyeccion.Observaciones,
            MotivoCancelacion = proyeccion.MotivoCancelacion,
            FechaActivacion = proyeccion.FechaActivacion,
            FechaCierre = proyeccion.FechaCierre,
            Saldo = proyeccion.MontoComprometido - proyeccion.MontoConsumido,
            OrdenesPeriodo = ordenes
        };
    }
}
