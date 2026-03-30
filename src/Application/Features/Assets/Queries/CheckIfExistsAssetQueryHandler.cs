using FluentResults;
using MediatR;

namespace Application.Features.Assets.Queries;

public class CheckIfExistsAssetQueryHandler : IRequestHandler<CheckIfExistsAssetQuery, Result<bool>>
{
    public Task<Result<bool>> Handle(CheckIfExistsAssetQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}