using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Practices;
using MediatR;

namespace Ayoos.Application.Practices.CreatePractice;

internal sealed class CreatePracticeCommandHandler(
    IPracticeProvisioner practiceProvisioner,
    ITenantRegistry tenantRegistry)
    : IRequestHandler<CreatePracticeCommand, PracticeModel>
{
    public async Task<PracticeModel> Handle(
        CreatePracticeCommand request,
        CancellationToken cancellationToken)
    {
        if (await tenantRegistry.IdentifierExistsAsync(request.Slug, cancellationToken))
        {
            throw new ConflictException(
                $"A practice with slug '{request.Slug}' already exists.");
        }

        var practice = Practice.Create(
            request.Name,
            request.Slug,
            request.TimeZone,
            request.Address.ToValueObject(),
            request.ContactEmail,
            request.ContactPhone,
            DateTimeOffset.UtcNow);

        await practiceProvisioner.ProvisionAsync(practice, cancellationToken);

        return practice.ToModel();
    }
}
