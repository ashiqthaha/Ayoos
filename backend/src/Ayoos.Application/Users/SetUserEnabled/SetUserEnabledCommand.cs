using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Users.SetUserEnabled;

public sealed record SetUserEnabledCommand(string UserId, bool Enabled)
    : IRequest;

public sealed class SetUserEnabledCommandValidator
    : AbstractValidator<SetUserEnabledCommand>
{
    public SetUserEnabledCommandValidator() =>
        RuleFor(command => command.UserId).NotEmpty();
}

internal sealed class SetUserEnabledCommandHandler(
    IUserManagementService userManagementService,
    ICurrentPracticeContext currentPractice)
    : IRequestHandler<SetUserEnabledCommand>
{
    public Task Handle(
        SetUserEnabledCommand request,
        CancellationToken cancellationToken) =>
        userManagementService.SetUserEnabledAsync(
            currentPractice.PracticeId,
            request.UserId,
            request.Enabled,
            cancellationToken);
}
