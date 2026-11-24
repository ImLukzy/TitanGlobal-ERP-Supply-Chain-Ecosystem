using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

public class CrearTransportistaTerceroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearTransportistaTerceroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarTransportistaTerceroRequestDto solicitud)
    {
        GuardarTransportistaTerceroRequestDto datos = ValidacionLogisticaExterna.Transportista(solicitud);
        string paisNormalizado = datos.Pais.ToLowerInvariant();
        string documentoNormalizado = datos.DocumentoFiscal.ToLowerInvariant();
        bool existe = await _unitOfWork.Repository<TransportistaTercero>().AsQueryable()
            .AnyAsync(t => t.Pais.Trim().ToLower() == paisNormalizado && t.DocumentoFiscal.Trim().ToLower() == documentoNormalizado);
        if (existe) { throw ValidacionLogisticaExterna.DocumentoDuplicado(datos.DocumentoFiscal, datos.Pais); }
        TransportistaTercero entidad = new TransportistaTercero
        {
            RazonSocial = datos.RazonSocial,
            DocumentoFiscal = datos.DocumentoFiscal,
            Pais = datos.Pais,
            TipoServicio = datos.TipoServicio,
            Contacto = datos.Contacto,
            Email = datos.Email,
            Telefono = datos.Telefono,
            Observaciones = datos.Observaciones,
            Activo = datos.Activo
        };
        await _unitOfWork.Repository<TransportistaTercero>().AddAsync(entidad);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw ValidacionLogisticaExterna.DocumentoDuplicado(datos.DocumentoFiscal, datos.Pais);
        }
        return entidad.Id;
    }
}
