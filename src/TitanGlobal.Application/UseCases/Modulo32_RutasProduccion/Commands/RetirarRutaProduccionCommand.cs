using MediatR;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion.Commands;

public class RetirarRutaProduccionCommand : IRequest<int>
{
    public int Id { get; set; }
}

public class RetirarRutaProduccionCommandHandler : IRequestHandler<RetirarRutaProduccionCommand, int>
{
    private readonly IUnitOfWork _unidad;

    private readonly IEscrituraRutaProduccion _escritura;

    public RetirarRutaProduccionCommandHandler(
        IUnitOfWork unidad,
        IEscrituraRutaProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> Handle(RetirarRutaProduccionCommand request, CancellationToken cancellationToken)
    {
        return await _escritura.EjecutarAsync(request.Id, RetirarAsync);

        async Task<int> RetirarAsync()
        {
            RutaProduccion ruta = await PersistenciaRutaProduccion.ObtenerAsync(_unidad, request.Id);
            if (ruta.Estado != ValidacionRutaProduccion.Activa)
            {
                throw ValidacionRutaProduccion.Conflicto("Solo se pueden retirar rutas de producción activas.");
            }

            ruta.Estado = ValidacionRutaProduccion.Retirada;
            await _unidad.CompleteAsync();
            return request.Id;
        }
    }
}
