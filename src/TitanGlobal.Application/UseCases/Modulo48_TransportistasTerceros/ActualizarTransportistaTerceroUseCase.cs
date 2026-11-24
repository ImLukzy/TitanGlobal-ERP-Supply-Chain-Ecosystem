using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

public class ActualizarTransportistaTerceroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarTransportistaTerceroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarTransportistaTerceroRequestDto solicitud)
    {
        TransportistaTercero? entidad = await _unitOfWork.Repository<TransportistaTercero>().AsQueryable().FirstOrDefaultAsync(t => t.Id == id);
        if (entidad is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El transportista {id} no existe."); }
        GuardarTransportistaTerceroRequestDto datos = ValidacionLogisticaExterna.Transportista(solicitud);
        string paisNormalizado = datos.Pais.ToLowerInvariant();
        string documentoNormalizado = datos.DocumentoFiscal.ToLowerInvariant();
        bool existe = await _unitOfWork.Repository<TransportistaTercero>().AsQueryable()
            .AnyAsync(t => t.Pais.Trim().ToLower() == paisNormalizado && t.DocumentoFiscal.Trim().ToLower() == documentoNormalizado && t.Id != id);
        if (existe) { throw ValidacionLogisticaExterna.DocumentoDuplicado(datos.DocumentoFiscal, datos.Pais); }
        entidad.RazonSocial = datos.RazonSocial;
        entidad.DocumentoFiscal = datos.DocumentoFiscal;
        entidad.Pais = datos.Pais;
        entidad.TipoServicio = datos.TipoServicio;
        entidad.Contacto = datos.Contacto;
        entidad.Email = datos.Email;
        entidad.Telefono = datos.Telefono;
        entidad.Observaciones = datos.Observaciones;
        entidad.Activo = datos.Activo;
        _unitOfWork.Repository<TransportistaTercero>().Update(entidad);
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
