using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class ResumenExpedientesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenExpedientesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenExpedientesDto> ExecuteAsync()
    {
        IQueryable<ExpedienteImportacion> expedientes = _unitOfWork.Repository<ExpedienteImportacion>().AsQueryable();
        int totalExpedientes = await expedientes.CountAsync();

        List<EstadoConteoDto> porEstado = new List<EstadoConteoDto>();
        foreach (string estado in ValidacionExpedienteImportacion.Estados)
        {
            int total = await expedientes.CountAsync(expediente => expediente.Estado == estado);
            porEstado.Add(new EstadoConteoDto { Estado = estado, Total = total });
        }

        int enCurso = porEstado
            .Where(conteo => conteo.Estado == ValidacionExpedienteImportacion.Programado
                || conteo.Estado == ValidacionExpedienteImportacion.EnTransito
                || conteo.Estado == ValidacionExpedienteImportacion.EnAduana)
            .Sum(conteo => conteo.Total);

        return new ResumenExpedientesDto
        {
            TotalExpedientes = totalExpedientes,
            EnCurso = enCurso,
            PorEstado = porEstado
        };
    }
}
