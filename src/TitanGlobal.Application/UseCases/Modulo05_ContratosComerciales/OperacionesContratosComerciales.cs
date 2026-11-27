using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales;

// Lógica compartida por los commands y queries de ContratosComerciales.
internal static class OperacionesContratosComerciales
{
    internal static async Task<ContratoComercial> ObtenerEntidadAsync(IUnitOfWork unitOfWork, int id)
    {
        ContratoComercial? contrato = await unitOfWork.Repository<ContratoComercial>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (contrato is null)
        {
            throw new ReglaNegocioException(404, $"El contrato {id} no existe.");
        }

        return contrato;
    }

    internal static void AplicarDatos(ContratoComercial contrato, GuardarContratoRequestDto datos)
    {
        contrato.ClienteId = datos.ClienteId;
        contrato.Titulo = datos.Titulo;
        contrato.FechaInicio = datos.FechaInicio;
        contrato.FechaFin = datos.FechaFin;
        contrato.MontoComprometido = datos.MontoComprometido;
        contrato.CondicionPago = datos.CondicionPago;
        contrato.Observaciones = datos.Observaciones;
    }

    internal static void ValidarEstado(ContratoComercial contrato, string esperado)
    {
        if (contrato.Estado != esperado)
        {
            throw ConflictoEstado(contrato);
        }
    }

    internal static ReglaNegocioException ConflictoEstado(ContratoComercial contrato)
    {
        return new ReglaNegocioException(409,
            $"El contrato {contrato.Numero} ya está {contrato.Estado.ToLowerInvariant()}.");
    }

    internal static ContratoListadoDto MapearListado(ContratoProyeccionDto proyeccion)
    {
        return new ContratoListadoDto
        {
            Id = proyeccion.Id,
            Numero = proyeccion.Numero,
            ClienteId = proyeccion.ClienteId,
            ClienteNombre = proyeccion.ClienteNombre,
            Titulo = proyeccion.Titulo,
            FechaInicio = proyeccion.FechaInicio,
            FechaFin = proyeccion.FechaFin,
            Estado = proyeccion.Estado,
            Vencido = proyeccion.Estado == "Vigente" && proyeccion.FechaFin < DateTime.UtcNow.Date,
            MontoComprometido = proyeccion.MontoComprometido,
            MontoConsumido = proyeccion.MontoConsumido,
            PorcentajeConsumido = Math.Round(proyeccion.MontoConsumido / proyeccion.MontoComprometido * 100m, 2)
        };
    }
}
