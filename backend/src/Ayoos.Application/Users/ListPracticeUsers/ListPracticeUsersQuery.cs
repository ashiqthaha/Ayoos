using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Users.ListPracticeUsers;

public sealed record ListPracticeUsersQuery()
    : IRequest<IReadOnlyList<ManagedUserModel>>;

internal sealed class ListPracticeUsersQueryHandler(
    IUserManagementService userManagementService,
    ICurrentPracticeContext currentPractice)
    : IRequestHandler<ListPracticeUsersQuery, IReadOnlyList<ManagedUserModel>>
{
    public Task<IReadOnlyList<ManagedUserModel>> Handle(
        ListPracticeUsersQuery request,
        CancellationToken cancellationToken) =>
        userManagementService.ListPracticeUsersAsync(
            currentPractice.PracticeId,
            cancellationToken);
}
