using Ayoos.Application.Providers;
using Ayoos.Application.Providers.SetAvailabilityRules;

namespace Ayoos.UnitTests;

public sealed class SetAvailabilityRulesCommandValidatorTests
{
    private readonly SetAvailabilityRulesCommandValidator _validator = new();

    [Fact]
    public async Task Rejects_overlapping_rules_for_the_same_day()
    {
        var effectiveFrom = new DateOnly(2026, 8, 1);
        var command = new SetAvailabilityRulesCommand(
            Guid.NewGuid(),
            [
                new AvailabilityRuleInput(
                    DayOfWeek.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(12, 0),
                    30,
                    effectiveFrom,
                    null),
                new AvailabilityRuleInput(
                    DayOfWeek.Monday,
                    new TimeOnly(11, 30),
                    new TimeOnly(15, 0),
                    30,
                    effectiveFrom,
                    null)
            ]);

        var result = await _validator.ValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == nameof(command.Rules) &&
                error.ErrorMessage.Contains("must not overlap"));
    }

    [Fact]
    public async Task Accepts_adjacent_rules_for_the_same_day()
    {
        var effectiveFrom = new DateOnly(2026, 8, 1);
        var command = new SetAvailabilityRulesCommand(
            Guid.NewGuid(),
            [
                new AvailabilityRuleInput(
                    DayOfWeek.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(12, 0),
                    30,
                    effectiveFrom,
                    null),
                new AvailabilityRuleInput(
                    DayOfWeek.Monday,
                    new TimeOnly(12, 0),
                    new TimeOnly(15, 0),
                    30,
                    effectiveFrom,
                    null)
            ]);

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }
}
