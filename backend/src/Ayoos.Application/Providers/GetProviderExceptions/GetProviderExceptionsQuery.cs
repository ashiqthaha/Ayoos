using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.GetProviderExceptions;

public sealed record GetProviderExceptionsQuery(
    Guid ProviderId,
    DateOnly FromDate,
    DateOnly ToDate) : IRequest<IReadOnlyList<AvailabilityExceptionModel>>;

public sealed class GetProviderExceptionsQueryValidator
    : AbstractValidator<GetProviderExceptionsQuery>
{
    public GetProviderExceptionsQueryValidator()
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

internal sealed class GetProviderExceptionsQueryHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<GetProviderExceptionsQuery, IReadOnlyList<AvailabilityExceptionModel>>
{
    public async Task<IReadOnlyList<AvailabilityExceptionModel>> Handle(
        GetProviderExceptionsQuery request,
        CancellationToken cancellationToken)
    {
        if (await providerRepository.GetByIdAsync(
            request.ProviderId,
            cancellationToken: cancellationToken) is null)
        {
            throw new NotFoundException($"Provider '{request.ProviderId}' was not found.");
        }

        var exceptions = await providerRepository.ListAvailabilityExceptionsAsync(
            request.ProviderId,
            request.FromDate,
            request.ToDate,
            cancellationToken);
        return exceptions.Select(exception => exception.ToModel()).ToArray();
    }
}
