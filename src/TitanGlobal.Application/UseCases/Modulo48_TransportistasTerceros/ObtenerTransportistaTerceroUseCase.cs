using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

public class ObtenerTransportistaTerceroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerTransportistaTerceroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TransportistaTerceroDetalleDto> ExecuteAsync(int id)
    {
        TransportistaTerceroDetalleDto? detalle = await _unitOfWork.Repository<TransportistaTercero>().AsQueryable().Where(t => t.Id == id)
            .Select(t => new TransportistaTerceroDetalleDto
            {
                Id = t.Id,
                RazonSocial = t.RazonSocial,
                DocumentoFiscal = t.DocumentoFiscal,
                Pais = t.Pais,
                TipoServicio = t.TipoServicio,
                Contacto = t.Contacto,
                Email = t.Email,
                Telefono = t.Telefono,
                Observaciones = t.Observaciones,
                Activo = t.Activo
            }).FirstOrDefaultAsync();
        if (detalle is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El transportista {id} no existe."); }
        return detalle;
    }
}
