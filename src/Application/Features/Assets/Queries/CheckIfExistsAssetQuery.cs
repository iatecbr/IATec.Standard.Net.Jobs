using FluentResults;
using MediatR;

namespace Application.Features.Assets.Queries;

public sealed class CheckIfExistsAssetQuery : IRequest<Result<bool>>;