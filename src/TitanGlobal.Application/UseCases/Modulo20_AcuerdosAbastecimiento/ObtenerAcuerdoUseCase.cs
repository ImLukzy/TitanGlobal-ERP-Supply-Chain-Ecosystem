using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class ObtenerAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerAcuerdoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AcuerdoDetalleDto> ExecuteAsync(int id)
    {
        AcuerdoDetalleDto? acuerdo = await _unitOfWork.Repository<AcuerdoAbastecimiento>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new AcuerdoDetalleDto
            {
                Id = entidad.Id,
                Numero = entidad.Numero,
                ProveedorId = entidad.ProveedorId,
                ProveedorNombre = entidad.Proveedor.RazonSocial,
                Descripcion = entidad.Descripcion,
                Estado = entidad.Estado,
                FechaInicio = entidad.FechaInicio,
                FechaFin = entidad.FechaFin,
                MontoMaximo = entidad.MontoMaximo,
                MontoComprometido = entidad.LineasAcuerdoAbastecimiento
                    .Sum(linea => linea.PrecioAcordado * linea.CantidadComprometida),
                MontoConsumido = entidad.LineasAcuerdoAbastecimiento
                    .Sum(linea => linea.PrecioAcordado * linea.CantidadConsumida),
                FechaRegistro = entidad.FechaRegistro
            })
            .SingleOrDefaultAsync();
        if (acuerdo is null)
        {
            throw ValidacionAcuerdoAbastecimiento.NoExiste(id);
        }

        acuerdo.Lineas = await _unitOfWork.Repository<LineaAcuerdoAbastecimiento>()
            .AsQueryable()
            .Where(linea => linea.AcuerdoId == id)
            .OrderBy(linea => linea.Id)
            .Select(linea => new LineaAcuerdoDto
            {
                Id = linea.Id,
                ProductoId = linea.ProductoId,
                ProductoNombre = linea.Producto.Nombre,
                PrecioAcordado = linea.PrecioAcordado,
                CantidadComprometida = linea.CantidadComprometida,
                CantidadConsumida = linea.CantidadConsumida,
                Saldo = linea.CantidadComprometida - linea.CantidadConsumida
            })
            .ToListAsync();

        return acuerdo;
    }
}
