using MediatR;
using TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Queries;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Commands;

public class ActivarContratoComercialCommand : IRequest<ContratoDetalleDto>
{
    public int Id { get; set; }
}

public class ActivarContratoComercialCommandHandler : IRequestHandler<ActivarContratoComercialCommand, ContratoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ISender _sender;

    public ActivarContratoComercialCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<ContratoDetalleDto> Handle(ActivarContratoComercialCommand request, CancellationToken cancellationToken)
    {
        ContratoComercial contrato = await OperacionesContratosComerciales.ObtenerEntidadAsync(_unitOfWork, request.Id);
        OperacionesContratosComerciales.ValidarEstado(contrato, "Borrador");
        if (contrato.FechaFin < DateTime.UtcNow.Date)
        {
            throw new ReglaNegocioException(409, $"El contrato {contrato.Numero} ya terminó su periodo.");
        }

        bool solapado = await _unitOfWork.Repository<ContratoComercial>().AsQueryable()
            .AnyAsync(otro => otro.Id != contrato.Id && otro.ClienteId == contrato.ClienteId
                && otro.Estado == "Vigente" && otro.FechaInicio <= contrato.FechaFin
                && otro.FechaFin >= contrato.FechaInicio);
        if (solapado)
        {
            ContratoComercial otro = await _unitOfWork.Repository<ContratoComercial>().AsQueryable()
                .Where(entidad => entidad.Id != contrato.Id && entidad.ClienteId == contrato.ClienteId
                    && entidad.Estado == "Vigente" && entidad.FechaInicio <= contrato.FechaFin
                    && entidad.FechaFin >= contrato.FechaInicio)
                .OrderBy(entidad => entidad.FechaInicio)
                .FirstAsync();
            throw new ReglaNegocioException(409,
                $"El cliente ya tiene el contrato vigente {otro.Numero} en ese periodo.");
        }

        contrato.Estado = "Vigente";
        contrato.FechaActivacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ContratoComercial>().Update(contrato);
        await _unitOfWork.CompleteAsync();
        return await _sender.Send(new ObtenerContratoComercialQuery { Id = request.Id }, cancellationToken);
    }
}
