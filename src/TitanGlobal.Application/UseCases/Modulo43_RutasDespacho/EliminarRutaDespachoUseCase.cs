using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho;

public class EliminarRutaDespachoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarRutaDespachoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        RutaDespacho? ruta = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().FirstOrDefaultAsync(r => r.Id == id);
        if (ruta is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La ruta de despacho {id} no existe."); }
        _unitOfWork.Repository<RutaDespacho>().Remove(ruta);
        try { await _unitOfWork.CompleteAsync(); }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede eliminar la ruta de despacho porque está en uso."); }
        return id;
    }
}
