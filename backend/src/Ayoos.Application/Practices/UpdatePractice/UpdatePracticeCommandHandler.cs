using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using MediatR;

namespace Ayoos.Application.Practices.UpdatePractice;

internal sealed class UpdatePracticeCommandHandler(
    IPracticeRepository practiceRepository,
    ITenantRegistry tenantRegistry)
    : IRequestHandler<UpdatePracticeCommand, PracticeModel>
{
    public async Task<PracticeModel> Handle(
        UpdatePracticeCommand request,
        CancellationToken cancellationToken)
    {
        var practice = await practiceRepository.GetBySlugAsync(
            request.CurrentSlug,
            cancellationToken);

        if (practice is null)
        {
            throw new NotFoundException(
                $"Practice '{request.CurrentSlug}' was not found.");
        }

        if (!string.Equals(request.CurrentSlug, request.Slug, StringComparison.Ordinal) &&
            await tenantRegistry.IdentifierExistsAsync(request.Slug, cancellationToken))
        {
            throw new ConflictException(
                $"A practice with slug '{request.Slug}' already exists.");
        }

        practice.Update(
            request.Name,
            request.Slug,
            request.TimeZone,
            request.Address.ToValueObject(),
            request.ContactEmail,
            request.ContactPhone,
            request.IsActive);

        await practiceRepository.SaveChangesAsync(cancellationToken);
        await tenantRegistry.UpdateAsync(
            practice.Id,
            practice.Slug,
            practice.Name,
            cancellationToken);

        return practice.ToModel();
    }
}
