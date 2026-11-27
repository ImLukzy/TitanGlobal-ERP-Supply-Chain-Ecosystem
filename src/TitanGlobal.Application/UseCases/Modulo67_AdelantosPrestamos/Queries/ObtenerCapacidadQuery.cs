using MediatR;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Queries;

public class ObtenerCapacidadQuery : IRequest<CapacidadPrestamoDto>
{
    public int EmpleadoId { get; set; }

    public DateOnly? Fecha { get; set; }
}

public class ObtenerCapacidadQueryHandler : IRequestHandler<ObtenerCapacidadQuery, CapacidadPrestamoDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerCapacidadQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CapacidadPrestamoDto> Handle(ObtenerCapacidadQuery request, CancellationToken cancellationToken)
    {
        await ValidacionPrestamo.ObtenerEmpleadoAsync(_unidad, request.EmpleadoId);
        DateOnly dia = request.Fecha ?? ValidacionPrestamo.Hoy();
        decimal sueldo = await ValidacionPrestamo.LeerSueldoVigenteAsync(_unidad, request.EmpleadoId, dia) ?? 0m;
        decimal neto = ValidacionPrestamo.NetoEstimado(sueldo);
        Dictionary<(int Anio, int Mes), decimal> comprometido =
            await ValidacionPrestamo.LeerComprometidoPorMesAsync(_unidad, request.EmpleadoId, dia.Year, dia.Month);
        return new CapacidadPrestamoDto
        {
            EmpleadoId = request.EmpleadoId,
            Fecha = dia,
            SueldoVigente = sueldo,
            NetoEstimado = neto,
            CuotaMaxima = ValidacionPrestamo.CuotaMaxima(neto),
            CuotasComprometidasPorMes = comprometido
                .OrderBy(par => par.Key.Anio)
                .ThenBy(par => par.Key.Mes)
                .Select(par => new CuotaComprometidaDto { Anio = par.Key.Anio, Mes = par.Key.Mes, Monto = par.Value })
                .ToList()
        };
    }
}
