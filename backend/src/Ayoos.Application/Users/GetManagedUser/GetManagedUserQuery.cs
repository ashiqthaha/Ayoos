using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Users.GetManagedUser;

public sealed record GetManagedUserQuery(string UserId)
    : IRequest<ManagedUserModel>;

public sealed class GetManagedUserQueryValidator
    : AbstractValidator<GetManagedUserQuery>
{
    public GetManagedUserQueryValidator() =>
        RuleFor(query => query.UserId).NotEmpty();
}

internal sealed class GetManagedUserQueryHandler(
    IUserManagementService userManagementService,
    ICurrentPracticeContext currentPractice)
    : IRequestHandler<GetManagedUserQuery, ManagedUserModel>
{
    public async Task<ManagedUserModel> Handle(
        GetManagedUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userManagementService.GetUserAsync(
            currentPractice.PracticeId,
            request.UserId,
            cancellationToken);

        return user ?? throw new NotFoundException(
            $"User '{request.UserId}' was not found.");
    }
}
