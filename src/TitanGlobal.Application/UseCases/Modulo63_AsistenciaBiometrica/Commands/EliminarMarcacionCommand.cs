using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Commands;

public class EliminarMarcacionCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarMarcacionCommandHandler : IRequestHandler<EliminarMarcacionCommand>
{
    private readonly IUnitOfWork _unidad;

    public EliminarMarcacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task Handle(EliminarMarcacionCommand request, CancellationToken cancellationToken)
    {
        Marcacion? marcacion = await _unidad.Repository<Marcacion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (marcacion is null)
        {
            throw ValidacionMarcacion.MarcacionInexistente(request.Id);
        }

        if (marcacion.Origen == CalculadoraJornada.OrigenReloj)
        {
            throw ValidacionMarcacion.Error409("Las marcaciones del reloj no se eliminan.");
        }

        DateOnly? fechaDecidida = await ValidacionMarcacion.BuscarJornadaDecididaAsync(
            _unidad,
            marcacion.EmpleadoId,
            FechasUtc.Normalizar(marcacion.Instante));
        if (fechaDecidida is not null)
        {
            throw ValidacionMarcacion.JornadaDecidida(fechaDecidida.Value);
        }

        _unidad.Repository<Marcacion>().Remove(marcacion);
        await _unidad.CompleteAsync();
    }
}
