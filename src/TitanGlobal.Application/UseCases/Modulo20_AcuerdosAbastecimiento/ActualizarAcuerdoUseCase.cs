using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class ActualizarAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarAcuerdoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, ActualizarAcuerdoRequestDto solicitud)
    {
        AcuerdoAbastecimiento? acuerdo = await _unitOfWork.Repository<AcuerdoAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (acuerdo is null)
        {
            throw ValidacionAcuerdoAbastecimiento.NoExiste(id);
        }

        if (acuerdo.Estado == ValidacionAcuerdoAbastecimiento.Cancelado)
        {
            throw ValidacionAcuerdoAbastecimiento.EstaCancelado(id);
        }

        string? descripcion = ValidacionAcuerdoAbastecimiento.NormalizarDescripcion(solicitud.Descripcion);
        ValidacionAcuerdoAbastecimiento.ValidarMontoMaximo(solicitud.MontoMaximo);
        ValidacionAcuerdoAbastecimiento.ValidarPeriodo(
            solicitud.FechaInicio,
            solicitud.FechaFin,
            out DateTime fechaInicio,
            out DateTime fechaFin);

        decimal compromiso = await _unitOfWork.Repository<LineaAcuerdoAbastecimiento>()
            .AsQueryable()
            .Where(linea => linea.AcuerdoId == id)
            .SumAsync(linea => (decimal?)(linea.PrecioAcordado * linea.CantidadComprometida)) ?? 0m;
        if (solicitud.MontoMaximo < compromiso)
        {
            throw ValidacionAcuerdoAbastecimiento.Error(
                "El monto máximo no puede ser menor al compromiso de las líneas.");
        }

        acuerdo.Descripcion = descripcion;
        acuerdo.FechaInicio = fechaInicio;
        acuerdo.FechaFin = fechaFin;
        acuerdo.MontoMaximo = solicitud.MontoMaximo;
        await _unitOfWork.CompleteAsync();

        return acuerdo.Id;
    }
}
