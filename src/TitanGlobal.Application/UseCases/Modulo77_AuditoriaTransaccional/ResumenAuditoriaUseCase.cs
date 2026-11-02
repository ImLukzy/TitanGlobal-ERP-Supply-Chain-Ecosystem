using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo77_AuditoriaTransaccional;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo77_AuditoriaTransaccional;

public class ResumenAuditoriaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenAuditoriaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenAuditoriaDto> ExecuteAsync()
    {
        IQueryable<RegistroAuditoria> consulta = _unitOfWork.Repository<RegistroAuditoria>().AsQueryable();
        int total = await consulta.CountAsync();
        int altas = await consulta.CountAsync(registro => registro.Accion == "Alta");
        int modificaciones = await consulta.CountAsync(registro => registro.Accion == "Modificacion");
        int bajas = await consulta.CountAsync(registro => registro.Accion == "Baja");
        List<EntidadAuditoriaDto> porEntidad = await consulta
            .GroupBy(registro => registro.Entidad)
            .Select(grupo => new EntidadAuditoriaDto
            {
                Entidad = grupo.Key,
                Registros = grupo.Count()
            })
            .OrderByDescending(item => item.Registros)
            .ThenBy(item => item.Entidad)
            .Take(10)
            .ToListAsync();

        DateTime hoy = DateTime.UtcNow.Date;
        DateTime inicio = hoy.AddDays(-6);
        List<DateTime> fechasRegistros = await consulta
            .Where(registro => registro.Fecha >= inicio)
            .Select(registro => registro.Fecha)
            .ToListAsync();
        List<DiaAuditoriaDto> ultimos7Dias = new List<DiaAuditoriaDto>();
        for (int desplazamiento = 0; desplazamiento < 7; desplazamiento++)
        {
            DateTime fecha = inicio.AddDays(desplazamiento);
            int cantidad = fechasRegistros.Count(fechaRegistro => fechaRegistro.Date == fecha);
            ultimos7Dias.Add(new DiaAuditoriaDto
            {
                Fecha = fecha,
                Registros = cantidad
            });
        }

        return new ResumenAuditoriaDto
        {
            Total = total,
            Altas = altas,
            Modificaciones = modificaciones,
            Bajas = bajas,
            PorEntidad = porEntidad,
            Ultimos7Dias = ultimos7Dias
        };
    }
}
