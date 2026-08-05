using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string? FirstName,
    string? LastName,
    string Role) : IRequest<CreatedUserModel>;

public sealed class CreateUserCommandValidator
    : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress();
        RuleFor(command => command.FirstName)
            .MaximumLength(UserValidation.MaximumNameLength)
            .When(command => command.FirstName is not null);
        RuleFor(command => command.LastName)
            .MaximumLength(UserValidation.MaximumNameLength)
            .When(command => command.LastName is not null);
        RuleFor(command => command.Role)
            .Must(role => UserValidation.AssignableRoles.Contains(role))
            .WithMessage("Role is not assignable.");
    }
}

internal sealed class CreateUserCommandHandler(
    IUserManagementService userManagementService,
    ICurrentPracticeContext currentPractice)
    : IRequestHandler<CreateUserCommand, CreatedUserModel>
{
    public Task<CreatedUserModel> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken) =>
        userManagementService.CreateUserAsync(
            currentPractice.PracticeId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Role,
            cancellationToken);
}
