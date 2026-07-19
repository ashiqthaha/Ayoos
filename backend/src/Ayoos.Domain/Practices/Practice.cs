using Ayoos.Domain.Common;

namespace Ayoos.Domain.Practices;

public sealed class Practice : Entity
{
    private Practice()
        : base(Guid.NewGuid())
    {
    }

    private Practice(
        Guid id,
        string name,
        string slug,
        string timeZone,
        Address address,
        string contactEmail,
        string contactPhone,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Name = name;
        Slug = slug;
        TimeZone = timeZone;
        Address = address;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string TimeZone { get; private set; } = string.Empty;

    public Address Address { get; private set; } = null!;

    public string ContactEmail { get; private set; } = string.Empty;

    public string ContactPhone { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsActive { get; private set; }

    public static Practice Create(
        string name,
        string slug,
        string timeZone,
        Address address,
        string contactEmail,
        string contactPhone,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(address);

        return new Practice(
            Guid.NewGuid(),
            name.Trim(),
            slug.Trim(),
            timeZone.Trim(),
            address,
            contactEmail.Trim(),
            Required(contactPhone, nameof(contactPhone)),
            createdAtUtc.ToUniversalTime());
    }

    public void Update(
        string name,
        string slug,
        string timeZone,
        Address address,
        string contactEmail,
        string contactPhone,
        bool isActive)
    {
        ArgumentNullException.ThrowIfNull(address);

        Name = name.Trim();
        Slug = slug.Trim();
        TimeZone = timeZone.Trim();
        Address = address;
        ContactEmail = contactEmail.Trim();
        ContactPhone = Required(contactPhone, nameof(contactPhone));
        IsActive = isActive;
    }

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}
