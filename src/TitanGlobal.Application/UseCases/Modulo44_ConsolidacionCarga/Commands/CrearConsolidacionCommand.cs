using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga.Commands;

public class CrearConsolidacionCommand : IRequest<int>
{
    public required GuardarConsolidacionRequestDto Solicitud { get; set; }
}

public class CrearConsolidacionCommandHandler : IRequestHandler<CrearConsolidacionCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public CrearConsolidacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(CrearConsolidacionCommand request, CancellationToken cancellationToken)
    {
        GuardarConsolidacionRequestDto datos = ValidacionCarga.Normalizar(request.Solicitud);
        string codigo = datos.Codigo.ToLowerInvariant();
        if (await _unidad.Repository<ConsolidacionCarga>().AsQueryable().AnyAsync(c => c.Codigo.Trim().ToLower() == codigo)) { throw ValidacionCarga.Duplicado(datos.Codigo); }
        ConsolidacionCarga carga = new ConsolidacionCarga { Codigo = datos.Codigo, Nombre = datos.Nombre, Observaciones = datos.Observaciones, Estado = "Borrador" };
        for (int i = 0; i < datos.Lineas!.Count; i++) { carga.Lineas.Add(ValidacionCarga.Entidad(datos.Lineas[i], i + 1)); }
        await _unidad.Repository<ConsolidacionCarga>().AddAsync(carga);
        await ValidacionCarga.GuardarAsync(_unidad, datos.Codigo);
        return carga.Id;
    }
}
