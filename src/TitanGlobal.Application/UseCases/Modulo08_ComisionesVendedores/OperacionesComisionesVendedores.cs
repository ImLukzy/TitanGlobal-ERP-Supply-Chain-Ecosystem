using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores;

// Lógica compartida por los commands y queries de ComisionesVendedores.
internal static class OperacionesComisionesVendedores
{
    internal static async Task<Vendedor> ObtenerVendedorEntidadAsync(IUnitOfWork unitOfWork, int id)
    {
        Vendedor? vendedor = await unitOfWork.Repository<Vendedor>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (vendedor is null)
        {
            throw new ReglaNegocioException(404, $"El vendedor {id} no existe.");
        }

        return vendedor;
    }

    internal static async Task ValidarCodigoUnicoAsync(IUnitOfWork unitOfWork, string codigo, int? id)
    {
        bool repetido = await unitOfWork.Repository<Vendedor>().AsQueryable()
            .AnyAsync(vendedor => vendedor.Codigo == codigo && (!id.HasValue || vendedor.Id != id.Value));
        if (repetido)
        {
            throw new ReglaNegocioException(409, $"Ya existe un vendedor con el código {codigo}.");
        }
    }

    internal static async Task ValidarMesNoLiquidadoAsync(IUnitOfWork unitOfWork, DateTime fecha)
    {
        DateTime fechaUtc = FechasUtc.Normalizar(fecha);
        int anio = fechaUtc.Year;
        int mes = fechaUtc.Month;
        bool liquidado = await unitOfWork.Repository<LiquidacionComision>().AsQueryable()
            .AnyAsync(liquidacion => liquidacion.Anio == anio && liquidacion.Mes == mes);
        if (liquidado)
        {
            throw new ReglaNegocioException(
                409,
                $"El mes {mes:D2}/{anio} ya está liquidado; no se pueden cambiar sus asignaciones.");
        }
    }

    internal static void AplicarDatos(Vendedor vendedor, GuardarVendedorRequestDto datos)
    {
        vendedor.Codigo = datos.Codigo;
        vendedor.Nombre = datos.Nombre;
        vendedor.Email = datos.Email;
        vendedor.PorcentajeComision = datos.PorcentajeComision;
        vendedor.Activo = datos.Activo;
    }

    internal static (DateTime Inicio, DateTime Fin) LimitesMes(int anio, int mes)
    {
        DateTime inicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        return (inicio, inicio.AddMonths(1));
    }

    internal static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}

internal sealed class ComisionAgrupada
{
    public int VendedorId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int Ordenes { get; set; }

    public decimal Ventas { get; set; }

    public decimal Porcentaje { get; set; }
}
