using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;

public class ActualizarSegmentoClienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarSegmentoClienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarSegmentoRequestDto solicitud)
    {
        SegmentoCliente? segmento = await _unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (segmento is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El segmento {id} no existe.");
        }

        SegmentoClienteNormalizado datos = ValidacionSegmentoCliente.Normalizar(solicitud);
        await PersistenciaSegmentoCliente.ValidarUnicidadAsync(_unitOfWork, datos.Codigo, datos.Prioridad, id);
        segmento.Codigo = datos.Codigo;
        segmento.Nombre = datos.Nombre;
        segmento.Descripcion = datos.Descripcion;
        segmento.Prioridad = datos.Prioridad;
        segmento.MontoMinimo = datos.MontoMinimo;
        segmento.OrdenesMinimas = datos.OrdenesMinimas;
        segmento.Activo = datos.Activo;
        _unitOfWork.Repository<SegmentoCliente>().Update(segmento);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (PersistenciaSegmentoCliente.EsViolacionUnica(excepcion))
        {
            throw PersistenciaSegmentoCliente.TraducirViolacionUnica(excepcion, datos.Codigo, datos.Prioridad);
        }

        return segmento.Id;
    }
}
