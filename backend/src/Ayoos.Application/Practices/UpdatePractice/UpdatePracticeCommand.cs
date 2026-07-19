using MediatR;

namespace Ayoos.Application.Practices.UpdatePractice;

public sealed record UpdatePracticeCommand(
    string CurrentSlug,
    string Name,
    string Slug,
    string TimeZone,
    PracticeAddressModel Address,
    string ContactEmail,
    string ContactPhone,
    bool IsActive) : IRequest<PracticeModel>;
