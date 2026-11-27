using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Queries;

public class ResumenReclamosPostventaQuery : IRequest<ResumenReclamosDto>
{
}

public class ResumenReclamosPostventaQueryHandler : IRequestHandler<ResumenReclamosPostventaQuery, ResumenReclamosDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenReclamosPostventaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenReclamosDto> Handle(ResumenReclamosPostventaQuery request, CancellationToken cancellationToken)
    {
        DateTime ahora = FechasUtc.Normalizar(DateTime.UtcNow);
        List<ReclamoPostventa> reclamos = await _unitOfWork.Repository<ReclamoPostventa>()
            .AsQueryable()
            .ToListAsync();
        List<ReclamoPostventa> completados = reclamos
            .Where(reclamo => (reclamo.Estado == "Resuelto" || reclamo.Estado == "Cerrado")
                && reclamo.FechaResolucion.HasValue)
            .ToList();
        List<ReclamoPostventa> aTiempo = completados
            .Where(reclamo => reclamo.FechaResolucion!.Value <= reclamo.FechaLimite)
            .ToList();
        decimal horasPromedio = completados.Count == 0
            ? 0m
            : decimal.Round(completados.Average(reclamo =>
                (decimal)(reclamo.FechaResolucion!.Value - reclamo.Fecha).TotalHours), 2, MidpointRounding.AwayFromZero);
        decimal cumplimiento = completados.Count == 0
            ? 0m
            : decimal.Round(aTiempo.Count * 100m / completados.Count, 2, MidpointRounding.AwayFromZero);
        List<TipoReclamosDto> porTipo = new List<TipoReclamosDto>();
        foreach (string tipo in OrdenTipos)
        {
            int cantidad = reclamos.Count(reclamo => reclamo.Tipo == tipo);
            if (cantidad > 0)
            {
                porTipo.Add(new TipoReclamosDto { Tipo = tipo, Reclamos = cantidad });
            }
        }

        return new ResumenReclamosDto
        {
            Total = reclamos.Count,
            Abiertos = reclamos.Count(reclamo => reclamo.Estado == "Abierto"),
            EnProceso = reclamos.Count(reclamo => reclamo.Estado == "EnProceso"),
            Resueltos = reclamos.Count(reclamo => reclamo.Estado == "Resuelto"),
            Cerrados = reclamos.Count(reclamo => reclamo.Estado == "Cerrado"),
            Rechazados = reclamos.Count(reclamo => reclamo.Estado == "Rechazado"),
            Vencidos = reclamos.Count(reclamo => OperacionesReclamosPostventa.EsVencido(reclamo, ahora)),
            PorTipo = porTipo,
            HorasPromedioResolucion = horasPromedio,
            CumplimientoPlazo = cumplimiento
        };
    }

    private static readonly string[] OrdenTipos = new[]
    {
        "Defecto",
        "Retraso",
        "Faltante",
        "Facturacion",
        "Otro"
    };
}
