using System.Globalization;
using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

public class DictaminarInspeccionProductoTerminadoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraInspeccionProductoTerminado _escritura;

    public DictaminarInspeccionProductoTerminadoUseCase(
        IUnitOfWork unidad, IEscrituraInspeccionProductoTerminado escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, DictaminarInspeccionProductoTerminadoRequestDto solicitud)
    {
        return await _escritura.EjecutarBloqueandoInspeccionAsync(id, DictaminarAsync);

        async Task<int> DictaminarAsync()
        {
            InspeccionProductoTerminado inspeccion = await ValidacionInspeccionProductoTerminado.ObtenerAsync(_unidad, id);
            if (inspeccion.Estado != ValidacionInspeccionProductoTerminado.Pendiente)
            {
                throw ValidacionInspeccionProductoTerminado.Conflicto("Solo se pueden dictaminar inspecciones pendientes.");
            }

            string resultado = NormalizarResultado(solicitud.Resultado);
            int noConforme = solicitud.CantidadNoConforme ?? -1;
            if (noConforme < 0)
            {
                throw ValidacionInspeccionProductoTerminado.Invalida("Las unidades no conformes no pueden ser negativas.");
            }

            if (noConforme > inspeccion.CantidadInspeccionada)
            {
                throw ValidacionInspeccionProductoTerminado.Invalida(
                    "Las unidades no conformes no pueden superar las inspeccionadas.");
            }

            string? observaciones = solicitud.Observaciones?.Trim();
            if (string.IsNullOrEmpty(observaciones))
            {
                observaciones = null;
            }
            else if (observaciones.Length > 500)
            {
                throw ValidacionInspeccionProductoTerminado.Invalida("Las observaciones no pueden superar 500 caracteres.");
            }

            bool supera = ValidacionInspeccionProductoTerminado.SuperaTolerancia(
                noConforme, inspeccion.CantidadInspeccionada, inspeccion.ToleranciaNoConformesPorcentaje);
            if (supera && resultado == ValidacionInspeccionProductoTerminado.Aprobada)
            {
                throw ValidacionInspeccionProductoTerminado.Conflicto(MensajeTolerancia(inspeccion, noConforme));
            }

            inspeccion.Estado = resultado;
            inspeccion.CantidadNoConforme = noConforme;
            inspeccion.Observaciones = observaciones;
            inspeccion.FechaDictamen = DateTime.UtcNow;
            await _unidad.CompleteAsync();
            return id;
        }
    }

    private static string NormalizarResultado(string? resultado)
    {
        string valor = resultado?.Trim() ?? string.Empty;
        if (valor.Equals(ValidacionInspeccionProductoTerminado.Aprobada, StringComparison.OrdinalIgnoreCase))
        {
            return ValidacionInspeccionProductoTerminado.Aprobada;
        }

        if (valor.Equals(ValidacionInspeccionProductoTerminado.Rechazada, StringComparison.OrdinalIgnoreCase))
        {
            return ValidacionInspeccionProductoTerminado.Rechazada;
        }

        throw ValidacionInspeccionProductoTerminado.Invalida("El resultado debe ser Aprobada o Rechazada.");
    }

    private static string MensajeTolerancia(InspeccionProductoTerminado inspeccion, int noConforme)
    {
        decimal porcentaje = ValidacionInspeccionProductoTerminado.Redondear(
            noConforme * 100m / inspeccion.CantidadInspeccionada);
        string textoPorcentaje = porcentaje.ToString("0.00", CultureInfo.InvariantCulture);
        string textoTolerancia = inspeccion.ToleranciaNoConformesPorcentaje.ToString("0.00", CultureInfo.InvariantCulture);
        return $"La inspección supera la tolerancia de no conformes ({textoPorcentaje} % > {textoTolerancia} %) "
            + "y solo puede rechazarse.";
    }
}
