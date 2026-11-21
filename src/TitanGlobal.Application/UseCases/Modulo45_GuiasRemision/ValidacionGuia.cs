using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

internal static class ValidacionGuia
{
    public static GuardarGuiaRequestDto Normalizar(GuardarGuiaRequestDto solicitud)
    {
        string serie = solicitud.Serie?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!System.Text.RegularExpressions.Regex.IsMatch(serie, "^[A-Z0-9]{1,8}$"))
        {
            throw Error("La serie debe contener entre 1 y 8 letras mayúsculas o dígitos.");
        }
        if (solicitud.Numero < 1 || solicitud.Numero > 99999999)
        {
            throw Error("El número debe estar entre 1 y 99999999.");
        }
        if (!solicitud.FechaInicioTraslado.HasValue || solicitud.FechaInicioTraslado.Value == default)
        {
            throw Error("La fecha de inicio del traslado es obligatoria.");
        }
        GuardarGuiaRequestDto datos = new GuardarGuiaRequestDto
        {
            Serie = serie,
            Numero = solicitud.Numero,
            FechaInicioTraslado = FechasUtc.Normalizar(solicitud.FechaInicioTraslado.Value),
            RemitenteNombre = Texto(solicitud.RemitenteNombre, "nombre del remitente", 200),
            RemitenteDocumento = Texto(solicitud.RemitenteDocumento, "documento del remitente", 20),
            DestinatarioNombre = Texto(solicitud.DestinatarioNombre, "nombre del destinatario", 200),
            DestinatarioDocumento = Texto(solicitud.DestinatarioDocumento, "documento del destinatario", 20),
            DireccionOrigen = Texto(solicitud.DireccionOrigen, "dirección de origen", 300),
            DireccionDestino = Texto(solicitud.DireccionDestino, "dirección de destino", 300),
            MotivoTraslado = Texto(solicitud.MotivoTraslado, "motivo del traslado", 200),
            Transportista = Texto(solicitud.Transportista, "transportista", 200),
            Conductor = Texto(solicitud.Conductor, "conductor", 100),
            Placa = Texto(solicitud.Placa, "placa", 20),
            PesoBrutoKg = solicitud.PesoBrutoKg,
            Observaciones = solicitud.Observaciones?.Trim(),
            Lineas = new List<LineaGuiaRequestDto>()
        };
        if (datos.PesoBrutoKg < 0.01m || datos.PesoBrutoKg > 9999999999.99m)
        {
            throw Error("El peso bruto debe estar entre 0.01 y 9999999999.99 kg.");
        }
        if (decimal.Round(datos.PesoBrutoKg, 2) != datos.PesoBrutoKg)
        {
            throw Error("El peso bruto no puede tener más de 2 decimales.");
        }
        if (datos.Observaciones?.Length > 500)
        {
            throw Error("Las observaciones no pueden superar 500 caracteres.");
        }
        if (string.IsNullOrEmpty(datos.Observaciones))
        {
            datos.Observaciones = null;
        }
        if (solicitud.Lineas is null || solicitud.Lineas.Count < 1 || solicitud.Lineas.Count > 100)
        {
            throw Error("La guía debe tener entre 1 y 100 líneas.");
        }
        for (int i = 0; i < solicitud.Lineas.Count; i++)
        {
            LineaGuiaRequestDto? linea = solicitud.Lineas[i];
            if (linea is null)
            {
                throw Error("La guía no puede contener líneas nulas.");
            }
            string prefijo = $"Línea {i + 1}: el campo ";
            LineaGuiaRequestDto nueva = new LineaGuiaRequestDto
            {
                Sku = Texto(linea.Sku, "SKU", 40, prefijo),
                Descripcion = Texto(linea.Descripcion, "descripción", 200, prefijo),
                UnidadMedida = Texto(linea.UnidadMedida, "unidad de medida", 10, prefijo),
                Cantidad = linea.Cantidad
            };
            if (linea.Cantidad < 0.001m || linea.Cantidad > 999999999.999m)
            {
                throw Error($"Línea {i + 1}: la cantidad debe estar entre 0.001 y 999999999.999.");
            }
            if (decimal.Round(linea.Cantidad, 3) != linea.Cantidad)
            {
                throw Error($"Línea {i + 1}: la cantidad no puede tener más de 3 decimales.");
            }
            datos.Lineas.Add(nueva);
        }
        return datos;
    }

    public static string Texto(string? valor, string etiqueta, int maximo, string prefijo = "El campo ")
    {
        string texto = valor?.Trim() ?? string.Empty;
        if (texto.Length == 0)
        {
            throw Error($"{prefijo}{etiqueta} es obligatorio.");
        }
        if (texto.Length > maximo)
        {
            throw Error($"{prefijo}{etiqueta} no puede superar {maximo} caracteres.");
        }
        return texto;
    }

    public static async Task<GuiaRemision> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        GuiaRemision? guia = await unidad.Repository<GuiaRemision>().AsQueryable()
            .Include(g => g.Lineas).FirstOrDefaultAsync(g => g.Id == id);
        if (guia is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La guía de remisión {id} no existe.");
        }
        return guia;
    }

    public static void ExigirBorrador(GuiaRemision guia, string accion)
    {
        if (guia.Estado != "Borrador")
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Solo se puede {accion} una guía en borrador.");
        }
    }

    public static ReglaNegocioException Error(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException Duplicado(string serie, int numero)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe una guía con la serie {serie} y el número {numero}.");
    }

    public static async Task ExigirNumeroLibreAsync(IUnitOfWork unidad, string serie, int numero, int id = 0)
    {
        if (await unidad.Repository<GuiaRemision>().AsQueryable().AnyAsync(g => g.Id != id && g.Serie == serie && g.Numero == numero))
        {
            throw Duplicado(serie, numero);
        }
    }

    public static async Task GuardarAsync(IUnitOfWork unidad, string serie, int numero)
    {
        try
        {
            await unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw Duplicado(serie, numero);
        }
    }

    public static void Asignar(GuiaRemision guia, GuardarGuiaRequestDto datos)
    {
        guia.Serie = datos.Serie;
        guia.Numero = datos.Numero;
        guia.FechaInicioTraslado = datos.FechaInicioTraslado!.Value;
        guia.RemitenteNombre = datos.RemitenteNombre;
        guia.RemitenteDocumento = datos.RemitenteDocumento;
        guia.DestinatarioNombre = datos.DestinatarioNombre;
        guia.DestinatarioDocumento = datos.DestinatarioDocumento;
        guia.DireccionOrigen = datos.DireccionOrigen;
        guia.DireccionDestino = datos.DireccionDestino;
        guia.MotivoTraslado = datos.MotivoTraslado;
        guia.Transportista = datos.Transportista;
        guia.Conductor = datos.Conductor;
        guia.Placa = datos.Placa;
        guia.PesoBrutoKg = datos.PesoBrutoKg;
        guia.Observaciones = datos.Observaciones;
    }

    public static LineaGuiaRemision Entidad(LineaGuiaRequestDto linea, int posicion)
    {
        return new LineaGuiaRemision
        {
            Posicion = posicion,
            Sku = linea.Sku,
            Descripcion = linea.Descripcion,
            UnidadMedida = linea.UnidadMedida,
            Cantidad = linea.Cantidad
        };
    }
}
