using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;

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
        catch (DbUpdateException excepcion) when (PersistenciaSegmentoCliente.EsViolacionUnica(excepcion))
        {
            throw PersistenciaSegmentoCliente.TraducirViolacionUnica(excepcion, datos.Codigo, datos.Prioridad);
        }

        return segmento.Id;
    }
}
