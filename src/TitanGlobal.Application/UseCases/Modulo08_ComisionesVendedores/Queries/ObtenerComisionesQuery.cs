using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Queries;

public class ObtenerComisionesQuery : IRequest<ComisionesMesDto>
{
    public int Anio { get; set; }

    public int Mes { get; set; }
}

public class ObtenerComisionesQueryHandler : IRequestHandler<ObtenerComisionesQuery, ComisionesMesDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerComisionesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ComisionesMesDto> Handle(ObtenerComisionesQuery request, CancellationToken cancellationToken)
    {
        ValidacionComisiones.ValidarPeriodo(request.Anio, request.Mes);
        List<LiquidacionComision> liquidaciones = await _unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .Where(liquidacion => liquidacion.Anio == request.Anio && liquidacion.Mes == request.Mes)
            .ToListAsync();
        if (liquidaciones.Count > 0)
        {
            List<int> vendedoresIds = liquidaciones.Select(liquidacion => liquidacion.VendedorId).ToList();
            List<Vendedor> vendedores = await _unitOfWork.Repository<Vendedor>().AsQueryable()
                .Where(vendedor => vendedoresIds.Contains(vendedor.Id)).ToListAsync();
            List<ComisionVendedorDto> filasGuardadas = liquidaciones
                .Select(liquidacion => MapearComisionLiquidada(liquidacion, vendedores.Single(vendedor => vendedor.Id == liquidacion.VendedorId)))
                .OrderBy(fila => fila.Codigo)
                .ToList();
            return CrearResumenComisiones(request.Anio, request.Mes, true, filasGuardadas);
        }

        (DateTime inicio, DateTime fin) = OperacionesComisionesVendedores.LimitesMes(request.Anio, request.Mes);
        // PATRÓN LINQ 4: GroupBy y Sum agrupan ventas y órdenes por vendedor en una sola consulta.
        List<ComisionAgrupada> agrupadas = await (
            from asignacion in _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            join orden in _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                on asignacion.OrdenCompraId equals orden.Id
            join vendedor in _unitOfWork.Repository<Vendedor>().AsQueryable()
                on asignacion.VendedorId equals vendedor.Id
            where orden.Fecha >= inicio && orden.Fecha < fin
            group new { orden, vendedor } by new { vendedor.Id, vendedor.Codigo, vendedor.Nombre, vendedor.PorcentajeComision } into grupo
            select new ComisionAgrupada
            {
                VendedorId = grupo.Key.Id,
                Codigo = grupo.Key.Codigo,
                Nombre = grupo.Key.Nombre,
                Ordenes = grupo.Count(),
                Ventas = grupo.Sum(item => item.orden.Total),
                Porcentaje = grupo.Key.PorcentajeComision
            }).ToListAsync();
        List<ComisionVendedorDto> filas = agrupadas.Select(grupo => new ComisionVendedorDto
        {
            VendedorId = grupo.VendedorId,
            Codigo = grupo.Codigo,
            Nombre = grupo.Nombre,
            Ordenes = grupo.Ordenes,
            Ventas = grupo.Ventas,
            Porcentaje = grupo.Porcentaje,
            Comision = Math.Round(grupo.Ventas * grupo.Porcentaje / 100m, 2, MidpointRounding.AwayFromZero),
            Estado = "Pendiente"
        }).OrderBy(fila => fila.Codigo).ToList();
        return CrearResumenComisiones(request.Anio, request.Mes, false, filas);
    }

    private static ComisionesMesDto CrearResumenComisiones(
        int anio,
        int mes,
        bool liquidado,
        List<ComisionVendedorDto> filas)
    {
        return new ComisionesMesDto
        {
            Anio = anio,
            Mes = mes,
            Liquidado = liquidado,
            Filas = filas,
            TotalVentas = filas.Sum(fila => fila.Ventas),
            TotalComision = filas.Sum(fila => fila.Comision)
        };
    }

    private static ComisionVendedorDto MapearComisionLiquidada(LiquidacionComision liquidacion, Vendedor vendedor)
    {
        return new ComisionVendedorDto
        {
            VendedorId = liquidacion.VendedorId,
            Codigo = vendedor.Codigo,
            Nombre = vendedor.Nombre,
            Ordenes = liquidacion.Ordenes,
            Ventas = liquidacion.Ventas,
            Porcentaje = liquidacion.Porcentaje,
            Comision = liquidacion.Comision,
            Estado = liquidacion.Estado,
            LiquidacionId = liquidacion.Id
        };
    }
}
