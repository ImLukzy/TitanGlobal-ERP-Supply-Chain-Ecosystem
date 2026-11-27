using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

public class ObtenerParametrosQuery : IRequest<ParametrosPlanillaDto>
{
}

public class ObtenerParametrosQueryHandler : IRequestHandler<ObtenerParametrosQuery, ParametrosPlanillaDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerParametrosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ParametrosPlanillaDto> Handle(ObtenerParametrosQuery request, CancellationToken cancellationToken)
    {
        return new ParametrosPlanillaDto
        {
            Parametros = await _unidad.Repository<ParametroPlanilla>().AsQueryable()
                .AsNoTracking()
                .OrderBy(parametro => parametro.Clave)
                .Select(parametro => new ParametroPlanillaDto { Clave = parametro.Clave, Valor = parametro.Valor, Descripcion = parametro.Descripcion })
                .ToListAsync(),
            Pensiones = await _unidad.Repository<ParametroPension>().AsQueryable()
                .AsNoTracking()
                .OrderBy(parametro => parametro.Codigo)
                .Select(parametro => new ParametroPensionDto
                {
                    Codigo = parametro.Codigo,
                    Sistema = parametro.Sistema,
                    TasaAporte = parametro.TasaAporte,
                    TasaPrimaSeguro = parametro.TasaPrimaSeguro,
                    TasaComision = parametro.TasaComision,
                    RemuneracionMaximaAsegurable = parametro.RemuneracionMaximaAsegurable
                })
                .ToListAsync(),
            Tramos = await _unidad.Repository<TramoQuintaCategoria>().AsQueryable()
                .AsNoTracking()
                .OrderBy(tramo => tramo.Orden)
                .Select(tramo => new TramoQuintaDto { Orden = tramo.Orden, HastaUit = tramo.HastaUit, Tasa = tramo.Tasa })
                .ToListAsync()
        };
    }
}
