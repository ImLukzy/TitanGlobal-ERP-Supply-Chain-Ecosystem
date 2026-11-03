using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores;

public class ListarConductoresUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarConductoresUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaConductoresDto> ExecuteAsync(
        string? busqueda,
        bool? habilitado,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de página debe ser mayor que cero.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<Conductor> consulta = _unitOfWork.Repository<Conductor>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLower();
            consulta = consulta.Where(conductor =>
                conductor.Nombre.ToLower().Contains(termino) ||
                conductor.DocumentoLicencia.ToLower().Contains(termino));
        }

        if (habilitado.HasValue)
        {
            consulta = consulta.Where(conductor => conductor.Habilitado == habilitado.Value);
        }

        int total = await consulta.CountAsync();

        List<ConductorListadoDto> items = await consulta
            .OrderBy(conductor => conductor.DocumentoLicencia)
            .ThenBy(conductor => conductor.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(conductor => new ConductorListadoDto
            {
                Id = conductor.Id,
                Nombre = conductor.Nombre,
                DocumentoLicencia = conductor.DocumentoLicencia,
                VencimientoLicencia = conductor.VencimientoLicencia,
                Habilitado = conductor.Habilitado,
                Activo = conductor.Activo
            })
            .ToListAsync();

        return new PaginaConductoresDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
