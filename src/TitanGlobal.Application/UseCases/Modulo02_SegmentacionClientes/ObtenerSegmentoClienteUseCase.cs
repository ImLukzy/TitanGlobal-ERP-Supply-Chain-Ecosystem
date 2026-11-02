using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;

public class ObtenerSegmentoClienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerSegmentoClienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SegmentoDetalleDto> ExecuteAsync(int id)
    {
        SegmentoCliente? segmento = await _unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (segmento is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El segmento {id} no existe.");
        }

        List<ClienteClasificadoDto> clientes = await ConsultaSegmentacionClientes.ObtenerClientesAsync(_unitOfWork);
        int asignados = clientes.Count(cliente => cliente.SegmentoId == id);
        return new SegmentoDetalleDto
        {
            Id = segmento.Id,
            Codigo = segmento.Codigo,
            Nombre = segmento.Nombre,
            Prioridad = segmento.Prioridad,
            MontoMinimo = segmento.MontoMinimo,
            OrdenesMinimas = segmento.OrdenesMinimas,
            Activo = segmento.Activo,
            Clientes = segmento.Activo ? asignados : 0,
            Descripcion = segmento.Descripcion
        };
    }
}
