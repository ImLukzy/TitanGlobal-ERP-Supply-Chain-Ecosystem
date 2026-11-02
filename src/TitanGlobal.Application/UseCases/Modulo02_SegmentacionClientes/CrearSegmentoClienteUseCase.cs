using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;

public class CrearSegmentoClienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearSegmentoClienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarSegmentoRequestDto solicitud)
    {
        SegmentoClienteNormalizado datos = ValidacionSegmentoCliente.Normalizar(solicitud);
        await PersistenciaSegmentoCliente.ValidarUnicidadAsync(_unitOfWork, datos.Codigo, datos.Prioridad, null);
        SegmentoCliente segmento = new SegmentoCliente
        {
            Codigo = datos.Codigo,
            Nombre = datos.Nombre,
            Descripcion = datos.Descripcion,
            Prioridad = datos.Prioridad,
            MontoMinimo = datos.MontoMinimo,
            OrdenesMinimas = datos.OrdenesMinimas,
            Activo = datos.Activo
        };
        await _unitOfWork.Repository<SegmentoCliente>().AddAsync(segmento);
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
