using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

internal static class ValidacionEntrega
{
    public static GuardarEntregaRequestDto Normalizar(GuardarEntregaRequestDto solicitud)
    {
        GuardarEntregaRequestDto datos = new GuardarEntregaRequestDto
        {
            Codigo = Texto(solicitud.Codigo, "código", 20),
            ReferenciaGuia = Texto(solicitud.ReferenciaGuia, "referencia de guía", 40),
            Destinatario = Texto(solicitud.Destinatario, "destinatario", 200),
            DireccionDestino = Texto(solicitud.DireccionDestino, "dirección de destino", 300),
        };
        if (!solicitud.FechaProgramada.HasValue || solicitud.FechaProgramada.Value == default)
        {
            throw Error("La fecha programada es obligatoria.");
        }
        datos.FechaProgramada = FechasUtc.Normalizar(solicitud.FechaProgramada.Value);
        datos.Observaciones = Opcional(solicitud.Observaciones, "Las observaciones no pueden superar 500 caracteres.");
        return datos;
    }

    public static RegistrarPodRequestDto NormalizarPod(RegistrarPodRequestDto solicitud)
    {
        return new RegistrarPodRequestDto
        {
            Receptor = Texto(solicitud.Receptor, "receptor", 100),
            DocumentoReceptor = Texto(solicitud.DocumentoReceptor, "documento del receptor", 20),
            ReferenciaConstancia = Texto(solicitud.ReferenciaConstancia, "referencia de constancia", 100),
            Observaciones = Opcional(solicitud.Observaciones, "Las observaciones de entrega no pueden superar 500 caracteres.")
        };
    }

    public static string Texto(string? entrada, string etiqueta, int maximo)
    {
        string texto = entrada?.Trim() ?? string.Empty;
        if (texto.Length == 0)
        {
            throw Error($"El campo {etiqueta} es obligatorio.");
        }
        if (texto.Length > maximo)
        {
            throw Error($"El campo {etiqueta} no puede superar {maximo} caracteres.");
        }
        return texto;
    }

    public static string? Opcional(string? entrada, string mensaje)
    {
        string? valor = entrada?.Trim();
        if (valor?.Length > 500)
        {
            throw Error(mensaje);
        }
        return string.IsNullOrEmpty(valor) ? null : valor;
    }

    public static ReglaNegocioException Error(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException Conflicto(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }

    public static void ExigirProgramada(EntregaDespacho entrega, string accion)
    {
        if (entrega.Estado != "Programada")
        {
            throw Conflicto($"Solo se puede {accion} una entrega programada.");
        }
    }

    public static async Task<EntregaDespacho> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        EntregaDespacho? entrega = await unidad.Repository<EntregaDespacho>().AsQueryable()
            .Include(e => e.Eventos).FirstOrDefaultAsync(e => e.Id == id);
        if (entrega is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La entrega {id} no existe.");
        }
        return entrega;
    }

    public static async Task ExigirCodigoLibreAsync(IUnitOfWork unidad, string codigo, int id = 0)
    {
        string normalizado = codigo.ToLowerInvariant();
        if (await unidad.Repository<EntregaDespacho>().AsQueryable().AnyAsync(e => e.Id != id && e.Codigo.Trim().ToLower() == normalizado))
        {
            throw Conflicto($"Ya existe una entrega con el código {codigo}.");
        }
    }

    public static async Task GuardarAsync(IUnitOfWork unidad, string codigo)
    {
        try
        {
            await unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw Conflicto($"Ya existe una entrega con el código {codigo}.");
        }
    }

    public static void Asignar(EntregaDespacho entrega, GuardarEntregaRequestDto datos)
    {
        entrega.Codigo = datos.Codigo;
        entrega.ReferenciaGuia = datos.ReferenciaGuia;
        entrega.Destinatario = datos.Destinatario;
        entrega.DireccionDestino = datos.DireccionDestino;
        entrega.FechaProgramada = datos.FechaProgramada!.Value;
        entrega.Observaciones = datos.Observaciones;
    }

    public static async Task GuardarTransicionAsync(IUnitOfWork unidad, EntregaDespacho entrega, DateTime fecha, string descripcion)
    {
        await unidad.Repository<EventoEntrega>().AddAsync(new EventoEntrega
        {
            EntregaDespachoId = entrega.Id,
            Estado = entrega.Estado,
            Fecha = fecha,
            Descripcion = descripcion
        });
        unidad.Repository<EntregaDespacho>().Update(entrega);
        await unidad.CompleteAsync();
    }
}
