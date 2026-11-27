using MediatR;
using TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Queries;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Commands;

public class LiquidarMesCommand : IRequest<ComisionesMesDto>
{
    public int Anio { get; set; }

    public int Mes { get; set; }
}

public class LiquidarMesCommandHandler : IRequestHandler<LiquidarMesCommand, ComisionesMesDto>
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ISender _sender;

    public LiquidarMesCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<ComisionesMesDto> Handle(LiquidarMesCommand request, CancellationToken cancellationToken)
    {
        ValidacionComisiones.ValidarPeriodo(request.Anio, request.Mes);
        (DateTime inicio, DateTime fin) = OperacionesComisionesVendedores.LimitesMes(request.Anio, request.Mes);
        bool yaLiquidado = await _unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .AnyAsync(liquidacion => liquidacion.Anio == request.Anio && liquidacion.Mes == request.Mes);
        if (yaLiquidado)
        {
            throw new ReglaNegocioException(409, $"El mes {request.Mes:D2}/{request.Anio} ya está liquidado.");
        }

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
        if (agrupadas.Count == 0)
        {
            throw new ReglaNegocioException(400, $"No hay ventas asignadas en {request.Mes:D2}/{request.Anio}.");
        }

        List<LiquidacionComision> nuevas = agrupadas.Select(grupo => new LiquidacionComision
        {
            VendedorId = grupo.VendedorId,
            Anio = request.Anio,
            Mes = request.Mes,
            Ordenes = grupo.Ordenes,
            Ventas = grupo.Ventas,
            Porcentaje = grupo.Porcentaje,
            Comision = Math.Round(grupo.Ventas * grupo.Porcentaje / 100m, 2, MidpointRounding.AwayFromZero),
            Estado = "Calculada",
            FechaCalculo = FechasUtc.Normalizar(DateTime.UtcNow)
        }).ToList();
        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            foreach (LiquidacionComision liquidacion in nuevas)
            {
                await _unitOfWork.Repository<LiquidacionComision>().AddAsync(liquidacion);
            }

            try
            {
                await _unitOfWork.CompleteAsync();
                transaccion.Complete();
            }
            catch (ViolacionRestriccionException excepcion) when (OperacionesComisionesVendedores.EsViolacionUnica(excepcion))
            {
                throw new ReglaNegocioException(409, $"El mes {request.Mes:D2}/{request.Anio} ya está liquidado.");
            }
        }

        return await _sender.Send(new ObtenerComisionesQuery { Anio = request.Anio, Mes = request.Mes }, cancellationToken);
    }
}
