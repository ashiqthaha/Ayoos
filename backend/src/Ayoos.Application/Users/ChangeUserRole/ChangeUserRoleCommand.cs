using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Users.ChangeUserRole;

public sealed record ChangeUserRoleCommand(string UserId, string Role)
    : IRequest;

public sealed class ChangeUserRoleCommandValidator
    : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Role)
            .Must(role => UserValidation.AssignableRoles.Contains(role))
            .WithMessage("Role is not assignable.");
    }
}

internal sealed class ChangeUserRoleCommandHandler(
    IUserManagementService userManagementService,
    ICurrentPracticeContext currentPractice)
    : IRequestHandler<ChangeUserRoleCommand>
{
    public Task Handle(
        ChangeUserRoleCommand request,
        CancellationToken cancellationToken) =>
        userManagementService.ChangeUserRoleAsync(
            currentPractice.PracticeId,
            request.UserId,
            request.Role,
            cancellationToken);
}
