using TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.UseCases;

public class ResumenCotizacionesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenCotizacionesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenCotizacionesDto> ExecuteAsync()
    {
        List<CotizacionDetalleDto> cotizaciones = await ConsultaCotizacion.ObtenerTodasAsync(_unitOfWork);
        int emitidas = cotizaciones.Count(cotizacion => cotizacion.Estado == "Emitida");
        int vencidas = cotizaciones.Count(cotizacion => cotizacion.Vencida);
        int aceptadas = cotizaciones.Count(cotizacion => cotizacion.Estado == "Aceptada");
        int rechazadas = cotizaciones.Count(cotizacion => cotizacion.Estado == "Rechazada");
        decimal montoVigente = cotizaciones
            .Where(cotizacion => cotizacion.Estado == "Emitida" && !cotizacion.Vencida)
            .Sum(cotizacion => cotizacion.Total);
        decimal montoAceptado = cotizaciones
            .Where(cotizacion => cotizacion.Estado == "Aceptada")
            .Sum(cotizacion => cotizacion.Total);
        int cotizacionesDecididas = aceptadas + rechazadas;
        decimal tasaAceptacion = cotizacionesDecididas == 0
            ? 0m
            : decimal.Round(aceptadas * 100m / cotizacionesDecididas, 2, MidpointRounding.AwayFromZero);
        List<ClienteCotizacionesDto> topClientes = cotizaciones
            .GroupBy(cotizacion => new { cotizacion.ClienteId, cotizacion.ClienteNombre })
            .Select(grupo => new ClienteCotizacionesDto
            {
                ClienteId = grupo.Key.ClienteId,
                Nombre = grupo.Key.ClienteNombre,
                Cotizaciones = grupo.Count(),
                MontoAceptado = grupo
                    .Where(cotizacion => cotizacion.Estado == "Aceptada")
                    .Sum(cotizacion => cotizacion.Total)
            })
            .OrderByDescending(cliente => cliente.MontoAceptado)
            .ThenBy(cliente => cliente.ClienteId)
            .Take(5)
            .ToList();

        return new ResumenCotizacionesDto
        {
            Total = cotizaciones.Count,
            Emitidas = emitidas,
            Vencidas = vencidas,
            Aceptadas = aceptadas,
            Rechazadas = rechazadas,
            MontoVigente = montoVigente,
            MontoAceptado = montoAceptado,
            TasaAceptacion = tasaAceptacion,
            TopClientes = topClientes
        };
    }
}
