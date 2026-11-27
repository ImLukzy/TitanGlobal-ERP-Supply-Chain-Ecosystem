using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Queries;

public class ObtenerPrestamoQuery : IRequest<PrestamoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerPrestamoQueryHandler : IRequestHandler<ObtenerPrestamoQuery, PrestamoDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerPrestamoQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PrestamoDetalleDto> Handle(ObtenerPrestamoQuery request, CancellationToken cancellationToken)
    {
        PrestamoDetalleDto? detalle = await _unidad.Repository<Prestamo>().AsQueryable()
            .AsNoTracking()
            .Where(prestamo => prestamo.Id == request.Id)
            .Select(prestamo => new PrestamoDetalleDto
            {
                Id = prestamo.Id,
                Numero = prestamo.Numero,
                EmpleadoId = prestamo.EmpleadoId,
                EmpleadoLegajo = prestamo.Empleado.Legajo,
                EmpleadoNombre = prestamo.Empleado.Apellidos + ", " + prestamo.Empleado.Nombres,
                Tipo = prestamo.Tipo,
                Monto = prestamo.Monto,
                NumeroCuotas = prestamo.NumeroCuotas,
                FechaOtorgamiento = prestamo.FechaOtorgamiento,
                PrimerAnio = prestamo.PrimerAnio,
                PrimerMes = prestamo.PrimerMes,
                Motivo = prestamo.Motivo,
                Saldo = prestamo.Saldo,
                Estado = prestamo.Estado,
                FechaAnulacion = prestamo.FechaAnulacion,
                MotivoAnulacion = prestamo.MotivoAnulacion
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw ValidacionPrestamo.PrestamoInexistente(request.Id);
        }

        detalle.Cuotas = await _unidad.Repository<CuotaPrestamo>().AsQueryable()
            .AsNoTracking()
            .Where(cuota => cuota.PrestamoId == request.Id)
            .OrderBy(cuota => cuota.NumeroCuota)
            .Select(cuota => new CuotaPrestamoDto
            {
                Id = cuota.Id,
                NumeroCuota = cuota.NumeroCuota,
                Anio = cuota.Anio,
                Mes = cuota.Mes,
                Monto = cuota.Monto,
                MontoDescontado = cuota.MontoDescontado,
                Estado = cuota.Estado
            })
            .ToListAsync();
        return detalle;
    }
}
