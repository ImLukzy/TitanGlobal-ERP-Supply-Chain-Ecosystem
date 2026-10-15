using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo05_ContratosComerciales.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo05_ContratosComerciales.UseCases;

public static class ValidacionContratoComercial
{
    private static readonly string[] Condiciones = { "Contado", "Credito30", "Credito60", "Credito90" };

    public static async Task<GuardarContratoRequestDto> ValidarAsync(
        GuardarContratoRequestDto solicitud,
        IUnitOfWork unitOfWork)
    {
        bool clienteExiste = await unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .AnyAsync(cliente => cliente.Id == solicitud.ClienteId);
        if (!clienteExiste)
        {
            throw new ReglaNegocioException(400, $"El cliente {solicitud.ClienteId} no existe.");
        }

        solicitud.Titulo = solicitud.Titulo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(solicitud.Titulo))
        {
            throw new ReglaNegocioException(400, "El título es obligatorio.");
        }

        if (solicitud.Titulo.Length > 200)
        {
            throw new ReglaNegocioException(400, "El título no puede superar 200 caracteres.");
        }

        solicitud.FechaInicio = DateTime.SpecifyKind(solicitud.FechaInicio.Date, DateTimeKind.Utc);
        solicitud.FechaFin = DateTime.SpecifyKind(solicitud.FechaFin.Date, DateTimeKind.Utc);
        if (solicitud.FechaFin <= solicitud.FechaInicio)
        {
            throw new ReglaNegocioException(400, "La fecha de fin debe ser posterior a la de inicio.");
        }

        if (solicitud.MontoComprometido <= 0)
        {
            throw new ReglaNegocioException(400, "El monto comprometido debe ser mayor que 0.");
        }

        string condicion = solicitud.CondicionPago?.Trim() ?? string.Empty;
        string? condicionCanonica = Condiciones.FirstOrDefault(valor =>
            string.Equals(valor, condicion, StringComparison.OrdinalIgnoreCase));
        if (condicionCanonica is null)
        {
            throw new ReglaNegocioException(400, "La condición de pago no es válida.");
        }

        solicitud.CondicionPago = condicionCanonica;
        solicitud.Observaciones = string.IsNullOrWhiteSpace(solicitud.Observaciones)
            ? null
            : solicitud.Observaciones.Trim();
        if (solicitud.Observaciones is not null && solicitud.Observaciones.Length > 500)
        {
            throw new ReglaNegocioException(400, "Las observaciones no pueden superar 500 caracteres.");
        }

        return solicitud;
    }

    public static string ValidarMotivo(string? motivo)
    {
        string valor = motivo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(400, "El motivo de cancelación es obligatorio.");
        }

        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(400, "El motivo de cancelación no puede superar 500 caracteres.");
        }

        return valor;
    }
}
