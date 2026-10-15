using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El segmento {id} no existe.");
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
        catch (DbUpdateException excepcion) when (PersistenciaSegmentoCliente.EsViolacionUnica(excepcion))
        {
            throw PersistenciaSegmentoCliente.TraducirViolacionUnica(excepcion, datos.Codigo, datos.Prioridad);
        }

        return segmento.Id;
    }
}
