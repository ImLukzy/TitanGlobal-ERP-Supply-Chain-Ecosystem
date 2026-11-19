using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class CrearLineaAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearLineaAcuerdoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int acuerdoId, CrearLineaRequestDto solicitud)
    {
        AcuerdoAbastecimiento? acuerdo = await _unitOfWork.Repository<AcuerdoAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == acuerdoId);
        if (acuerdo is null)
        {
            throw ValidacionAcuerdoAbastecimiento.NoExiste(acuerdoId);
        }

        if (acuerdo.Estado == ValidacionAcuerdoAbastecimiento.Cancelado)
        {
            throw ValidacionAcuerdoAbastecimiento.EstaCancelado(acuerdoId);
        }

        if (solicitud.ProductoId <= 0)
        {
            throw ValidacionAcuerdoAbastecimiento.Error("El producto es obligatorio.");
        }

        ValidacionAcuerdoAbastecimiento.ValidarPrecio(solicitud.PrecioAcordado);
        ValidacionAcuerdoAbastecimiento.ValidarCantidadComprometida(solicitud.CantidadComprometida);

        bool productoExiste = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .AnyAsync(producto => producto.Id == solicitud.ProductoId);
        if (!productoExiste)
        {
            throw ValidacionAcuerdoAbastecimiento.Error($"El producto {solicitud.ProductoId} no existe.");
        }

        bool productoRepetido = await _unitOfWork.Repository<LineaAcuerdoAbastecimiento>()
            .AsQueryable()
            .AnyAsync(linea => linea.AcuerdoId == acuerdoId && linea.ProductoId == solicitud.ProductoId);
        if (productoRepetido)
        {
            throw CrearConflictoProducto(solicitud.ProductoId, acuerdoId);
        }

        decimal compromisoActual = await _unitOfWork.Repository<LineaAcuerdoAbastecimiento>()
            .AsQueryable()
            .Where(linea => linea.AcuerdoId == acuerdoId)
            .SumAsync(linea => (decimal?)(linea.PrecioAcordado * linea.CantidadComprometida)) ?? 0m;
        decimal compromisoNuevo = solicitud.PrecioAcordado * solicitud.CantidadComprometida;
        if (compromisoActual + compromisoNuevo > acuerdo.MontoMaximo)
        {
            throw ValidacionAcuerdoAbastecimiento.Error(
                "El compromiso de las líneas supera el monto máximo del acuerdo.");
        }

        LineaAcuerdoAbastecimiento lineaNueva = new LineaAcuerdoAbastecimiento
        {
            AcuerdoId = acuerdoId,
            ProductoId = solicitud.ProductoId,
            PrecioAcordado = solicitud.PrecioAcordado,
            CantidadComprometida = solicitud.CantidadComprometida,
            CantidadConsumida = 0
        };
        await _unitOfWork.Repository<LineaAcuerdoAbastecimiento>().AddAsync(lineaNueva);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoProducto(solicitud.ProductoId, acuerdoId);
        }

        return lineaNueva.Id;
    }

    private static ReglaNegocioException CrearConflictoProducto(int productoId, int acuerdoId)
    {
        return ValidacionAcuerdoAbastecimiento.Conflicto(
            $"El producto {productoId} ya está en el acuerdo {acuerdoId}.");
    }
}
