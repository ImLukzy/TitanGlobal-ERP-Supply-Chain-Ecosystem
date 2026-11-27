using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga.Commands;

public class ActualizarConsolidacionCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarConsolidacionRequestDto Solicitud { get; set; }
}

public class ActualizarConsolidacionCommandHandler : IRequestHandler<ActualizarConsolidacionCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public ActualizarConsolidacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(ActualizarConsolidacionCommand request, CancellationToken cancellationToken)
    {
        ConsolidacionCarga carga = await ValidacionCarga.ObtenerAsync(_unidad, request.Id);
        ValidacionCarga.ExigirBorrador(carga, "editar");
        GuardarConsolidacionRequestDto datos = ValidacionCarga.Normalizar(request.Solicitud);
        string codigo = datos.Codigo.ToLowerInvariant();
        if (await _unidad.Repository<ConsolidacionCarga>().AsQueryable().AnyAsync(c => c.Id != request.Id && c.Codigo.Trim().ToLower() == codigo)) { throw ValidacionCarga.Duplicado(datos.Codigo); }
        foreach (LineaConsolidacionCarga anterior in carga.Lineas.ToList()) { _unidad.Repository<LineaConsolidacionCarga>().Remove(anterior); }
        carga.Codigo = datos.Codigo; carga.Nombre = datos.Nombre; carga.Observaciones = datos.Observaciones;
        for (int i = 0; i < datos.Lineas!.Count; i++) { LineaConsolidacionCarga nueva = ValidacionCarga.Entidad(datos.Lineas[i], i + 1); nueva.ConsolidacionCargaId = request.Id; await _unidad.Repository<LineaConsolidacionCarga>().AddAsync(nueva); }
        _unidad.Repository<ConsolidacionCarga>().Update(carga);
        await ValidacionCarga.GuardarAsync(_unidad, datos.Codigo);
        return carga.Id;
    }
}
