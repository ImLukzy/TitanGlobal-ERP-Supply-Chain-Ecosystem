using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Queries;

public class ResumenContratosComercialesQuery : IRequest<ResumenContratosDto>
{
}

public class ResumenContratosComercialesQueryHandler : IRequestHandler<ResumenContratosComercialesQuery, ResumenContratosDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenContratosComercialesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenContratosDto> Handle(ResumenContratosComercialesQuery request, CancellationToken cancellationToken)
    {
        List<ContratoComercial> contratos = await _unitOfWork.Repository<ContratoComercial>()
            .AsQueryable().ToListAsync();
        List<ContratoProyeccionDto> vigentes = await _unitOfWork.Repository<ContratoComercial>()
            .AsQueryable()
            .Where(contrato => contrato.Estado == "Vigente")
            .Select(contrato => new ContratoProyeccionDto
            {
                Id = contrato.Id,
                Numero = contrato.Numero,
                ClienteId = contrato.ClienteId,
                ClienteNombre = contrato.Cliente.Nombre,
                Titulo = contrato.Titulo,
                FechaInicio = contrato.FechaInicio,
                FechaFin = contrato.FechaFin,
                Estado = contrato.Estado,
                MontoComprometido = contrato.MontoComprometido,
                MontoConsumido = _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                    .Where(orden => orden.ClienteId == contrato.ClienteId
                        && orden.Fecha >= contrato.FechaInicio && orden.Fecha < contrato.FechaFin.AddDays(1))
                    .Sum(orden => (decimal?)orden.Total) ?? 0m
            })
            .ToListAsync();
        decimal comprometido = vigentes.Sum(contrato => contrato.MontoComprometido);
        decimal consumido = vigentes.Sum(contrato => contrato.MontoConsumido);

        List<CondicionContratosDto> porCondicion = new List<CondicionContratosDto>();
        foreach (string condicion in CondicionesOrdenadas)
        {
            int cantidad = contratos.Count(contrato => contrato.CondicionPago == condicion);
            if (cantidad > 0)
            {
                porCondicion.Add(new CondicionContratosDto { CondicionPago = condicion, Contratos = cantidad });
            }
        }

        return new ResumenContratosDto
        {
            Total = contratos.Count,
            Borradores = contratos.Count(contrato => contrato.Estado == "Borrador"),
            Vigentes = vigentes.Count,
            Vencidos = vigentes.Count(contrato => contrato.FechaFin < DateTime.UtcNow.Date),
            Finalizados = contratos.Count(contrato => contrato.Estado == "Finalizado"),
            Cancelados = contratos.Count(contrato => contrato.Estado == "Cancelado"),
            MontoComprometidoVigente = comprometido,
            MontoConsumidoVigente = consumido,
            PorcentajeConsumoVigente = comprometido == 0m ? 0m : Math.Round(consumido / comprometido * 100m, 2),
            PorCondicion = porCondicion
        };
    }

    private static readonly string[] CondicionesOrdenadas = { "Contado", "Credito30", "Credito60", "Credito90" };
}
