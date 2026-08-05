using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Users.ResetUserPassword;

public sealed record ResetUserPasswordCommand(string UserId)
    : IRequest<string>;

public sealed class ResetUserPasswordCommandValidator
    : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator() =>
        RuleFor(command => command.UserId).NotEmpty();
}

internal sealed class ResetUserPasswordCommandHandler(
    IUserManagementService userManagementService,
    ICurrentPracticeContext currentPractice)
    : IRequestHandler<ResetUserPasswordCommand, string>
{
    public Task<string> Handle(
        ResetUserPasswordCommand request,
        CancellationToken cancellationToken) =>
        userManagementService.ResetUserPasswordAsync(
            currentPractice.PracticeId,
            request.UserId,
            cancellationToken);
}
