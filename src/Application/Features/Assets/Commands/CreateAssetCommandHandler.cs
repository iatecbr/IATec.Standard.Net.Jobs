using FluentResults;
using MediatR;

namespace Application.Features.Assets.Commands;

public class CreateAssetCommandHandler : IRequestHandler<CreateAssetCommand, Result>
{
    public Task<Result> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}