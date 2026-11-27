using MediatR;

namespace TitanGlobal.Application.UseCases.Modulo00_Sistema.Queries;

public class ConsultarSaludQuery : IRequest<object>
{
}

public class ConsultarSaludQueryHandler : IRequestHandler<ConsultarSaludQuery, object>
{
    public Task<object> Handle(ConsultarSaludQuery request, CancellationToken cancellationToken)
    {
        object estado = new { Estado = "OK" };
        return Task.FromResult(estado);
    }
}
