using Ayoos.Application.Practices;
using MediatR;

namespace Ayoos.Application.Practices.CreatePractice;

public sealed record CreatePracticeCommand(
    string Name,
    string Slug,
    string TimeZone,
    PracticeAddressModel Address,
    string ContactEmail,
    string ContactPhone) : IRequest<PracticeModel>;
