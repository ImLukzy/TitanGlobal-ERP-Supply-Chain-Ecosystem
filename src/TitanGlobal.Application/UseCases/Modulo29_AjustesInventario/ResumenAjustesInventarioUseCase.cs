using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo29_AjustesInventario;

public class ResumenAjustesInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenAjustesInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenAjustesInventarioDto> ExecuteAsync(
        int? almacenId,
        DateTime? fechaDesde,
        DateTime? fechaHasta)
    {
        ValidacionAjustesInventario.ValidarRangoFechas(fechaDesde, fechaHasta);

        IQueryable<AjusteInventario> consulta = _unitOfWork.Repository<AjusteInventario>().AsQueryable();

        if (almacenId.HasValue)
        {
            int filtroAlmacen = almacenId.Value;
            consulta = consulta.Where(registro => registro.AlmacenId == filtroAlmacen);
        }

        if (fechaDesde.HasValue)
        {
            DateTime desde = FechasUtc.Normalizar(fechaDesde.Value);
            consulta = consulta.Where(registro => registro.Fecha >= desde);
        }

        if (fechaHasta.HasValue)
        {
            DateTime hasta = FechasUtc.Normalizar(fechaHasta.Value);
            consulta = consulta.Where(registro => registro.Fecha <= hasta);
        }

        List<AjusteInventario> ajustes = await consulta.ToListAsync();
        int totalIncrementos = ajustes.Count(registro => registro.Tipo == "Incremento");
        int totalDecrementos = ajustes.Count(registro => registro.Tipo == "Decremento");
        decimal valorIncrementos = Math.Round(
            ajustes
                .Where(registro => registro.Tipo == "Incremento")
                .Sum(registro => registro.Cantidad * registro.CostoUnitario),
            2);
        decimal valorDecrementos = Math.Round(
            ajustes
                .Where(registro => registro.Tipo == "Decremento")
                .Sum(registro => registro.Cantidad * registro.CostoUnitario),
            2);

        return new ResumenAjustesInventarioDto
        {
            TotalAjustes = ajustes.Count,
            TotalIncrementos = totalIncrementos,
            TotalDecrementos = totalDecrementos,
            ValorIncrementos = valorIncrementos,
            ValorDecrementos = valorDecrementos,
            ValorNeto = Math.Round(valorIncrementos - valorDecrementos, 2)
        };
    }
}
