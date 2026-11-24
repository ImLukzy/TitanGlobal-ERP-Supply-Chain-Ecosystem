using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

public class EliminarTransportistaTerceroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarTransportistaTerceroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        TransportistaTercero? entidad = await _unitOfWork.Repository<TransportistaTercero>().AsQueryable().FirstOrDefaultAsync(t => t.Id == id);
        if (entidad is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El transportista {id} no existe."); }
        if (await _unitOfWork.Repository<FleteTercero>().AsQueryable().AnyAsync(f => f.TransportistaTerceroId == id))
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede eliminar un transportista con fletes registrados.");
        }
        _unitOfWork.Repository<TransportistaTercero>().Remove(entidad);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede eliminar un transportista con fletes registrados.");
        }
        return id;
    }
}
