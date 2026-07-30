using Ayoos.Domain.Common;

namespace Ayoos.Domain.Providers;

public sealed class Provider : Entity
{
    private readonly List<AvailabilityRule> _availabilityRules = [];
    private readonly List<AvailabilityException> _availabilityExceptions = [];

    private Provider()
        : base(Guid.NewGuid())
    {
    }

    private Provider(
        Guid id,
        Guid practiceId,
        string firstName,
        string lastName,
        string credentials,
        string specialty,
        string email,
        string phone,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        PracticeId = practiceId;
        FirstName = firstName;
        LastName = lastName;
        Credentials = credentials;
        Specialty = specialty;
        Email = email;
        Phone = phone;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public Guid PracticeId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Credentials { get; private set; } = string.Empty;

    public string Specialty { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<AvailabilityRule> AvailabilityRules => _availabilityRules;

    public IReadOnlyCollection<AvailabilityException> AvailabilityExceptions =>
        _availabilityExceptions;

    public static Provider Create(
        Guid practiceId,
        string firstName,
        string lastName,
        string credentials,
        string specialty,
        string email,
        string phone,
        DateTimeOffset createdAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(practiceId, Guid.Empty);

        return new Provider(
            Guid.NewGuid(),
            practiceId,
            Required(firstName, nameof(firstName)),
            Required(lastName, nameof(lastName)),
            Required(credentials, nameof(credentials)),
            Required(specialty, nameof(specialty)),
            Required(email, nameof(email)),
            Required(phone, nameof(phone)),
            createdAtUtc.ToUniversalTime());
    }

    public void Update(
        string firstName,
        string lastName,
        string credentials,
        string specialty,
        string email,
        string phone)
    {
        FirstName = Required(firstName, nameof(firstName));
        LastName = Required(lastName, nameof(lastName));
        Credentials = Required(credentials, nameof(credentials));
        Specialty = Required(specialty, nameof(specialty));
        Email = Required(email, nameof(email));
        Phone = Required(phone, nameof(phone));
    }

    public void Deactivate() => IsActive = false;

    public void ReplaceAvailabilityRules(IEnumerable<AvailabilityRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var replacements = rules.ToList();

        if (replacements.Any(rule => rule.ProviderId != Id))
        {
            throw new ArgumentException(
                "Every availability rule must belong to this provider.",
                nameof(rules));
        }

        var overlaps = replacements
            .GroupBy(rule => rule.DayOfWeek)
            .Any(group =>
            {
                var ordered = group.OrderBy(rule => rule.StartTime).ToArray();
                return ordered
                    .Zip(ordered.Skip(1))
                    .Any(pair => pair.First.EndTime > pair.Second.StartTime);
            });

        if (overlaps)
        {
            throw new ArgumentException(
                "Availability rules for the same provider and day must not overlap.",
                nameof(rules));
        }

        _availabilityRules.Clear();
        _availabilityRules.AddRange(replacements);
    }

    public void AddAvailabilityException(AvailabilityException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.ProviderId != Id)
        {
            throw new ArgumentException(
                "The availability exception must belong to this provider.",
                nameof(exception));
        }

        if (_availabilityExceptions.Any(item => item.Date == exception.Date))
        {
            throw new InvalidOperationException(
                $"An availability exception already exists for {exception.Date:yyyy-MM-dd}.");
        }

        _availabilityExceptions.Add(exception);
    }

    public bool RemoveAvailabilityException(Guid exceptionId)
    {
        var exception = _availabilityExceptions.SingleOrDefault(item => item.Id == exceptionId);
        return exception is not null && _availabilityExceptions.Remove(exception);
    }

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}
