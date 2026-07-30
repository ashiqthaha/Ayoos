using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Domain.Providers;
using FluentValidation;
using MediatR;

namespace Ayoos.Application.Providers.SetAvailabilityRules;

public sealed record SetAvailabilityRulesCommand(
    Guid ProviderId,
    IReadOnlyList<AvailabilityRuleInput> Rules)
    : IRequest<IReadOnlyList<AvailabilityRuleModel>>;

public sealed class SetAvailabilityRulesCommandValidator
    : AbstractValidator<SetAvailabilityRulesCommand>
{
    public SetAvailabilityRulesCommandValidator()
    {
        RuleFor(command => command.ProviderId).NotEmpty();
        RuleFor(command => command.Rules).NotNull();
        RuleForEach(command => command.Rules)
            .SetValidator(new AvailabilityRuleInputValidator());
        RuleFor(command => command.Rules)
            .Must(NotContainOverlaps)
            .WithMessage(
                "Availability rules for the same provider and day must not overlap.");
    }

    private static bool NotContainOverlaps(IReadOnlyList<AvailabilityRuleInput>? rules)
    {
        if (rules is null)
        {
            return true;
        }

        return rules
            .GroupBy(rule => rule.DayOfWeek)
            .All(group =>
            {
                var ordered = group.OrderBy(rule => rule.StartTime).ToArray();
                return !ordered
                    .Zip(ordered.Skip(1))
                    .Any(pair => pair.First.EndTime > pair.Second.StartTime);
            });
    }
}

internal sealed class SetAvailabilityRulesCommandHandler(
    IProviderRepository providerRepository)
    : IRequestHandler<SetAvailabilityRulesCommand, IReadOnlyList<AvailabilityRuleModel>>
{
    public async Task<IReadOnlyList<AvailabilityRuleModel>> Handle(
        SetAvailabilityRulesCommand request,
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

        var existingRules = provider.AvailabilityRules.ToArray();
        var rules = request.Rules
            .Select(rule => AvailabilityRule.Create(
                provider.Id,
                rule.DayOfWeek,
                rule.StartTime,
                rule.EndTime,
                rule.SlotDurationMinutes,
                rule.EffectiveFrom,
                rule.EffectiveTo))
            .ToArray();

        provider.ReplaceAvailabilityRules(rules);
        providerRepository.RemoveAvailabilityRules(existingRules);
        await providerRepository.AddAvailabilityRulesAsync(rules, cancellationToken);
        await providerRepository.SaveChangesAsync(cancellationToken);

        return rules.Select(rule => rule.ToModel()).ToArray();
    }
}
