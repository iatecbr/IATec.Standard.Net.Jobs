using FluentResults;
using MediatR;

namespace Application.Features.Assets.Commands;

public sealed class CreateAssetCommand : IRequest<Result>;