using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Commands;

public class PagarLiquidacionCommand : IRequest<LiquidacionDto>
{
    public int Id { get; set; }
}

public class PagarLiquidacionCommandHandler : IRequestHandler<PagarLiquidacionCommand, LiquidacionDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public PagarLiquidacionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LiquidacionDto> Handle(PagarLiquidacionCommand request, CancellationToken cancellationToken)
    {
        LiquidacionComision? liquidacion = await _unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (liquidacion is null)
        {
            throw new ReglaNegocioException(404, $"La liquidación {request.Id} no existe.");
        }

        if (liquidacion.Estado == "Pagada")
        {
            throw new ReglaNegocioException(409, $"La liquidación {request.Id} ya está pagada.");
        }

        liquidacion.Estado = "Pagada";
        liquidacion.FechaPago = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<LiquidacionComision>().Update(liquidacion);
        await _unitOfWork.CompleteAsync();
        Vendedor vendedor = await OperacionesComisionesVendedores.ObtenerVendedorEntidadAsync(_unitOfWork, liquidacion.VendedorId);
        return MapearLiquidacion(liquidacion, vendedor);
    }

    private static LiquidacionDto MapearLiquidacion(LiquidacionComision liquidacion, Vendedor vendedor)
    {
        return new LiquidacionDto
        {
            Id = liquidacion.Id,
            VendedorId = liquidacion.VendedorId,
            Codigo = vendedor.Codigo,
            Nombre = vendedor.Nombre,
            Anio = liquidacion.Anio,
            Mes = liquidacion.Mes,
            Ordenes = liquidacion.Ordenes,
            Ventas = liquidacion.Ventas,
            Porcentaje = liquidacion.Porcentaje,
            Comision = liquidacion.Comision,
            Estado = liquidacion.Estado,
            FechaCalculo = liquidacion.FechaCalculo,
            FechaPago = liquidacion.FechaPago
        };
    }
}
