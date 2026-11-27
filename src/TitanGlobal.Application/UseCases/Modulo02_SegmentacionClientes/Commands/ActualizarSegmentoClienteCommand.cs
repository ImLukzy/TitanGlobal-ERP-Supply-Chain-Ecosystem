using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes.Commands;

public class ActualizarSegmentoClienteCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarSegmentoRequestDto Solicitud { get; set; }
}

public class ActualizarSegmentoClienteCommandHandler : IRequestHandler<ActualizarSegmentoClienteCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarSegmentoClienteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarSegmentoClienteCommand request, CancellationToken cancellationToken)
    {
        SegmentoCliente? segmento = await _unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (segmento is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El segmento {request.Id} no existe.");
        }

        SegmentoClienteNormalizado datos = ValidacionSegmentoCliente.Normalizar(request.Solicitud);
        await PersistenciaSegmentoCliente.ValidarUnicidadAsync(_unitOfWork, datos.Codigo, datos.Prioridad, request.Id);
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
