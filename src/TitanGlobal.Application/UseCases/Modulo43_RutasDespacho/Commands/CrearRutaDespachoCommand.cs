using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho.Commands;

public class CrearRutaDespachoCommand : IRequest<int>
{
    public required GuardarRutaDespachoRequestDto Solicitud { get; set; }
}

public class CrearRutaDespachoCommandHandler : IRequestHandler<CrearRutaDespachoCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearRutaDespachoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearRutaDespachoCommand request, CancellationToken cancellationToken)
    {
        GuardarRutaDespachoRequestDto datos = ValidacionRutaDespacho.Normalizar(request.Solicitud);
        string codigo = datos.Codigo.ToLowerInvariant();
        bool duplicado = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().AnyAsync(r => r.Codigo.Trim().ToLower() == codigo);
        if (duplicado) { throw ValidacionRutaDespacho.Duplicado(datos.Codigo); }
        RutaDespacho ruta = new RutaDespacho
        {
            Codigo = datos.Codigo,
            Nombre = datos.Nombre,
            Zona = datos.Zona,
            Origen = datos.Origen,
            Destino = datos.Destino,
            DistanciaKm = datos.DistanciaKm,
            PeajeEstimado = datos.PeajeEstimado,
            Moneda = datos.Moneda,
            Activo = datos.Activo,
            Observaciones = datos.Observaciones
        };
        await _unitOfWork.Repository<RutaDespacho>().AddAsync(ruta);
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
