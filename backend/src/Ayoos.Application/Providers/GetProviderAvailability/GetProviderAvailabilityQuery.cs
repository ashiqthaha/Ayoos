using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.GetProviderAvailability;

public sealed record GetProviderAvailabilityQuery(
    Guid ProviderId,
    DateOnly FromDate,
    DateOnly ToDate) : IRequest<IReadOnlyList<AvailabilitySlotModel>>;

public sealed class GetProviderAvailabilityQueryValidator
    : AbstractValidator<GetProviderAvailabilityQuery>
{
    public GetProviderAvailabilityQueryValidator()
    {
        RuleFor(query => query.ProviderId).NotEmpty();
        RuleFor(query => query.ToDate)
            .GreaterThanOrEqualTo(query => query.FromDate)
            .WithMessage("ToDate must be on or after FromDate.");
        RuleFor(query => query)
            .Must(query => query.ToDate.DayNumber - query.FromDate.DayNumber <= 366)
            .WithName("ToDate")
            .WithMessage("The requested date range cannot exceed 366 days.");
    }
}

internal sealed class GetProviderAvailabilityQueryHandler(
    IProviderRepository providerRepository,
    AvailabilitySlotGenerator slotGenerator)
    : IRequestHandler<GetProviderAvailabilityQuery, IReadOnlyList<AvailabilitySlotModel>>
{
    public async Task<IReadOnlyList<AvailabilitySlotModel>> Handle(
        GetProviderAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var provider = await providerRepository.GetByIdAsync(
            request.ProviderId,
            includeAvailability: true,
            cancellationToken);

        if (provider is null)
        {
            throw new NotFoundException($"Provider '{request.ProviderId}' was not found.");
        }

        if (!provider.IsActive)
        {
            return [];
        }

        return slotGenerator.Generate(
            provider.AvailabilityRules,
            provider.AvailabilityExceptions,
            request.FromDate,
            request.ToDate);
    }
}
