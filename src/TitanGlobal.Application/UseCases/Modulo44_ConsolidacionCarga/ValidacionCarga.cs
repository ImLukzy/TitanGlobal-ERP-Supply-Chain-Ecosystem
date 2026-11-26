using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;

internal static class ValidacionCarga
{
    public static string Texto(string? dato, int maximo, string requerido, string largo)
    {
        string valor = dato?.Trim() ?? string.Empty;
        if (valor.Length == 0) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, requerido); }
        if (valor.Length > maximo) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, largo); }
        return valor;
    }

    public static GuardarConsolidacionRequestDto Normalizar(GuardarConsolidacionRequestDto datos)
    {
        string codigo = Texto(datos.Codigo, 20, "El código de la consolidación es obligatorio.", "El código de la consolidación no puede superar 20 caracteres.");
        string nombre = Texto(datos.Nombre, 100, "El nombre de la consolidación es obligatorio.", "El nombre de la consolidación no puede superar 100 caracteres.");
        string observaciones = datos.Observaciones?.Trim() ?? string.Empty;
        if (observaciones.Length > 500) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Las observaciones no pueden superar 500 caracteres."); }
        if (datos.Lineas is null || datos.Lineas.Count < 1 || datos.Lineas.Count > 100) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La consolidación debe tener entre 1 y 100 líneas."); }
        List<LineaCargaRequestDto> lineas = new List<LineaCargaRequestDto>();
        for (int indice = 0; indice < datos.Lineas.Count; indice++)
        {
            LineaCargaRequestDto? linea = datos.Lineas[indice];
            if (linea is null) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La consolidación no puede contener líneas nulas."); }
            int i = indice + 1;
            string sku = Texto(linea.Sku, 40, $"Línea {i}: el SKU es obligatorio.", $"Línea {i}: el SKU no puede superar 40 caracteres.");
            string descripcion = Texto(linea.Descripcion, 200, $"Línea {i}: la descripción es obligatoria.", $"Línea {i}: la descripción no puede superar 200 caracteres.");
            if (linea.Cantidad < 1 || linea.Cantidad > 1000000) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: la cantidad debe estar entre 1 y 1000000."); }
            if (linea.CantidadRecogida < 0 || linea.CantidadRecogida > 1000000) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: la cantidad recogida debe estar entre 0 y 1000000."); }
            if (linea.CantidadRecogida > linea.Cantidad) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: la cantidad recogida no puede superar la cantidad prevista."); }
            if (linea.UnidadesPorBulto < 0 || linea.UnidadesPorBulto > 1000000) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: las unidades por bulto debe estar entre 0 y 1000000."); }
            if (linea.LargoCm < 0 || linea.LargoCm > 9999.99m) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: el largo debe estar entre 0 y 9999.99."); }
            if (decimal.Round(linea.LargoCm, 2) != linea.LargoCm) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: el largo no puede tener más de 2 decimales."); }
            if (linea.AnchoCm < 0 || linea.AnchoCm > 9999.99m) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: el ancho debe estar entre 0 y 9999.99."); }
            if (decimal.Round(linea.AnchoCm, 2) != linea.AnchoCm) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: el ancho no puede tener más de 2 decimales."); }
            if (linea.AltoCm < 0 || linea.AltoCm > 9999.99m) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: el alto debe estar entre 0 y 9999.99."); }
            if (decimal.Round(linea.AltoCm, 2) != linea.AltoCm) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: el alto no puede tener más de 2 decimales."); }
            if (linea.PesoBultoKg < 0 || linea.PesoBultoKg > 999999.99m) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: el peso por bulto debe estar entre 0 y 999999.99."); }
            if (decimal.Round(linea.PesoBultoKg, 2) != linea.PesoBultoKg) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"Línea {i}: el peso por bulto no puede tener más de 2 decimales."); }
            lineas.Add(new LineaCargaRequestDto
            {
                Sku = sku,
                Descripcion = descripcion,
                Cantidad = linea.Cantidad,
                CantidadRecogida = linea.CantidadRecogida,
                UnidadesPorBulto = linea.UnidadesPorBulto,
                LargoCm = linea.LargoCm,
                AnchoCm = linea.AnchoCm,
                AltoCm = linea.AltoCm,
                PesoBultoKg = linea.PesoBultoKg
            });
        }
        return new GuardarConsolidacionRequestDto { Codigo = codigo, Nombre = nombre, Observaciones = observaciones.Length == 0 ? null : observaciones, Lineas = lineas };
    }

    public static void ExigirBorrador(ConsolidacionCarga carga, string accion)
    { if (carga.Estado != "Borrador") { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Solo se puede {accion} una consolidación en borrador."); } }

    public static ReglaNegocioException Duplicado(string codigo)
    { return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe una consolidación con el código {codigo}."); }

    public static async Task<ConsolidacionCarga> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        ConsolidacionCarga? carga = await unidad.Repository<ConsolidacionCarga>().AsQueryable().Include(c => c.Lineas).FirstOrDefaultAsync(c => c.Id == id);
        if (carga is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La consolidación {id} no existe."); }
        return carga;
    }

    public static async Task GuardarAsync(IUnitOfWork unidad, string codigo)
    {
        try { await unidad.CompleteAsync(); }
        catch (ViolacionRestriccionException error) when (error.Tipo == TipoViolacionRestriccion.Unicidad) { throw Duplicado(codigo); }
    }

    public static LineaConsolidacionCarga Entidad(LineaCargaRequestDto linea, int posicion)
    {
        return new LineaConsolidacionCarga
        {
            Posicion = posicion,
            Sku = linea.Sku,
            Descripcion = linea.Descripcion,
            Cantidad = linea.Cantidad,
            CantidadRecogida = linea.CantidadRecogida,
            UnidadesPorBulto = linea.UnidadesPorBulto,
            LargoCm = linea.LargoCm,
            AnchoCm = linea.AnchoCm,
            AltoCm = linea.AltoCm,
            PesoBultoKg = linea.PesoBultoKg
        };
    }
}
