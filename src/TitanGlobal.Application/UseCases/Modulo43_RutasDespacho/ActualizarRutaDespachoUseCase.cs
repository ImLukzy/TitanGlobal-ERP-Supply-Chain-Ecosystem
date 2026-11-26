using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho;

public class ActualizarRutaDespachoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarRutaDespachoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarRutaDespachoRequestDto solicitud)
    {
        RutaDespacho? ruta = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().FirstOrDefaultAsync(r => r.Id == id);
        if (ruta is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La ruta de despacho {id} no existe."); }
        GuardarRutaDespachoRequestDto datos = ValidacionRutaDespacho.Normalizar(solicitud);
        string codigo = datos.Codigo.ToLowerInvariant();
        bool duplicado = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().AnyAsync(r => r.Id != id && r.Codigo.Trim().ToLower() == codigo);
        if (duplicado) { throw ValidacionRutaDespacho.Duplicado(datos.Codigo); }
        ruta.Codigo = datos.Codigo;
        ruta.Nombre = datos.Nombre;
        ruta.Zona = datos.Zona;
        ruta.Origen = datos.Origen;
        ruta.Destino = datos.Destino;
        ruta.DistanciaKm = datos.DistanciaKm;
        ruta.PeajeEstimado = datos.PeajeEstimado;
        ruta.Moneda = datos.Moneda;
        ruta.Activo = datos.Activo;
        ruta.Observaciones = datos.Observaciones;
        _unitOfWork.Repository<RutaDespacho>().Update(ruta);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw ValidacionRutaDespacho.Duplicado(datos.Codigo);
        }
        return ruta.Id;
    }
}
