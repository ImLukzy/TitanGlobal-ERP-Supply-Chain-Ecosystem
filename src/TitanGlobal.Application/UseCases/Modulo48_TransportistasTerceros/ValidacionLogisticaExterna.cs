using System.Net.Mail;
using System.Text.RegularExpressions;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

internal static class ValidacionLogisticaExterna
{
    public static string Obligatorio(string? texto, int maximo, string obligatorio, string longitud)
    {
        string valor = texto?.Trim() ?? string.Empty;
        if (valor.Length == 0) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, obligatorio); }
        if (valor.Length > maximo) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, longitud); }
        return valor;
    }

    public static string? Opcional(string? texto, int maximo, string mensaje)
    {
        string valor = texto?.Trim() ?? string.Empty;
        if (valor.Length > maximo) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje); }
        return valor.Length == 0 ? null : valor;
    }

    public static string TipoServicio(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        foreach (string permitido in new[] { "3PL", "Flete", "Ambos" })
        {
            if (permitido.Equals(valor, StringComparison.OrdinalIgnoreCase)) { return permitido; }
        }
        throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tipo de servicio debe ser 3PL, Flete o Ambos.");
    }

    public static void Pagina(int pagina, int tamanoPagina)
    {
        if (pagina < 1) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de página debe ser mayor que cero."); }
        if (tamanoPagina < 1 || tamanoPagina > 100) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100."); }
    }

    public static GuardarTransportistaTerceroRequestDto Transportista(GuardarTransportistaTerceroRequestDto solicitud)
    {
        string razon = Obligatorio(solicitud.RazonSocial, 200, "La razón social es obligatoria.", "La razón social no puede superar 200 caracteres.");
        string documento = Obligatorio(solicitud.DocumentoFiscal, 30, "El documento fiscal es obligatorio.", "El documento fiscal no puede superar 30 caracteres.");
        string pais = Obligatorio(solicitud.Pais, 100, "El país es obligatorio.", "El país no puede superar 100 caracteres.");
        string tipo = TipoServicio(solicitud.TipoServicio);
        string? contacto = Opcional(solicitud.Contacto, 100, "El contacto no puede superar 100 caracteres.");
        string? email = Opcional(solicitud.Email, 150, "El email no puede superar 150 caracteres.");
        if (email is not null && !MailAddress.TryCreate(email, out _)) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El email no es válido."); }
        string? telefono = Opcional(solicitud.Telefono, 20, "El teléfono no puede superar 20 caracteres.");
        string? observaciones = Opcional(solicitud.Observaciones, 500, "Las observaciones no pueden superar 500 caracteres.");
        return new GuardarTransportistaTerceroRequestDto
        {
            RazonSocial = razon,
            DocumentoFiscal = documento,
            Pais = pais,
            TipoServicio = tipo,
            Contacto = contacto,
            Email = email,
            Telefono = telefono,
            Observaciones = observaciones,
            Activo = solicitud.Activo
        };
    }

    public static RegistrarFleteTerceroRequestDto Flete(RegistrarFleteTerceroRequestDto solicitud)
    {
        if (solicitud.TransportistaId <= 0) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El transportista es obligatorio."); }
        string referencia = Obligatorio(solicitud.Referencia, 30, "La referencia del flete es obligatoria.", "La referencia del flete no puede superar 30 caracteres.");
        if (solicitud.Fecha == DateTime.MinValue || solicitud.Fecha == DateTime.MaxValue) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha del flete no es válida."); }
        DateTime fecha = FechasUtc.Normalizar(solicitud.Fecha);
        string origen = Obligatorio(solicitud.Origen, 120, "El origen es obligatorio.", "El origen no puede superar 120 caracteres.");
        string destino = Obligatorio(solicitud.Destino, 120, "El destino es obligatorio.", "El destino no puede superar 120 caracteres.");
        if (solicitud.Importe <= 0) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El importe debe ser mayor que cero."); }
        if (solicitud.Importe > 9999999999999999.99m) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El importe no puede superar 9999999999999999.99."); }
        if (decimal.Round(solicitud.Importe, 2) != solicitud.Importe) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El importe no puede tener más de 2 decimales."); }
        string moneda = solicitud.Moneda?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!Regex.IsMatch(moneda, "^[A-Z]{3}$")) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La moneda debe tener tres letras."); }
        string? observaciones = Opcional(solicitud.Observaciones, 500, "Las observaciones no pueden superar 500 caracteres.");
        return new RegistrarFleteTerceroRequestDto
        {
            TransportistaId = solicitud.TransportistaId,
            Referencia = referencia,
            Fecha = fecha,
            Origen = origen,
            Destino = destino,
            Importe = solicitud.Importe,
            Moneda = moneda,
            Observaciones = observaciones
        };
    }

    public static ReglaNegocioException DocumentoDuplicado(string documento, string pais)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe un transportista con el documento fiscal {documento} en {pais}.");
    }

    public static ReglaNegocioException ReferenciaDuplicada(string referencia)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe un flete con la referencia {referencia} para este transportista.");
    }
}
