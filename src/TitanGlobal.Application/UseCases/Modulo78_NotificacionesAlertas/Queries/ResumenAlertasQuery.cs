using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo78_NotificacionesAlertas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo78_NotificacionesAlertas.Queries;

public class ResumenAlertasQuery : IRequest<ResumenAlertasDto>
{
}

public class ResumenAlertasQueryHandler : IRequestHandler<ResumenAlertasQuery, ResumenAlertasDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenAlertasQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenAlertasDto> Handle(ResumenAlertasQuery request, CancellationToken cancellationToken)
    {
        List<AlertaDto> alertas = await OperacionesAlertas.ObtenerAlertasAsync(_unitOfWork);
        List<string> clavesDescartadas = await _unitOfWork.Repository<AlertaDescartada>()
            .AsQueryable()
            .Select(alerta => alerta.Clave)
            .ToListAsync();
        HashSet<string> conjuntoDescartadas = clavesDescartadas.ToHashSet(StringComparer.Ordinal);
        int descartadas = 0;
        foreach (AlertaDto alerta in alertas)
        {
            alerta.Descartada = conjuntoDescartadas.Contains(alerta.Clave);
            if (alerta.Descartada)
            {
                descartadas++;
            }
        }

        List<AlertaDto> activas = alertas.Where(alerta => !alerta.Descartada).ToList();
        List<TipoAlertasDto> porTipo = new List<TipoAlertasDto>();
        foreach (string tipo in GeneradorAlertas.ObtenerTipos())
        {
            int cantidad = activas.Count(alerta => alerta.Tipo == tipo);
            if (cantidad > 0)
            {
                porTipo.Add(new TipoAlertasDto { Tipo = tipo, Alertas = cantidad });
            }
        }

        return new ResumenAlertasDto
        {
            Total = activas.Count,
            Altas = activas.Count(alerta => alerta.Severidad == "Alta"),
            Medias = activas.Count(alerta => alerta.Severidad == "Media"),
            Bajas = activas.Count(alerta => alerta.Severidad == "Baja"),
            Descartadas = descartadas,
            PorTipo = porTipo
        };
    }
}
