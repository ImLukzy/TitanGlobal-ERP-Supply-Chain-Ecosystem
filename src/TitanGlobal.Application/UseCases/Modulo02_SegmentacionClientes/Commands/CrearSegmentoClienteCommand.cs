using MediatR;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes.Commands;

public class CrearSegmentoClienteCommand : IRequest<int>
{
    public required GuardarSegmentoRequestDto Solicitud { get; set; }
}

public class CrearSegmentoClienteCommandHandler : IRequestHandler<CrearSegmentoClienteCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearSegmentoClienteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearSegmentoClienteCommand request, CancellationToken cancellationToken)
    {
        SegmentoClienteNormalizado datos = ValidacionSegmentoCliente.Normalizar(request.Solicitud);
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
