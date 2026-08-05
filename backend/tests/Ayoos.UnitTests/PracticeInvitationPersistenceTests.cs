using Ayoos.Application.Common.Security;
using Ayoos.Domain.PracticeInvitations;
using Ayoos.Infrastructure.Persistence;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ayoos.UnitTests;

public sealed class PracticeInvitationPersistenceTests
{
    [Fact]
    public async Task Invitation_is_global_and_visible_across_tenant_contexts()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var databaseRoot = new InMemoryDatabaseRoot();
        var invitation = PracticeInvitation.Create(
            PracticeInvitationToken.Hash("global-token"),
            "admin@example.com",
            "keycloak-user",
            DateTimeOffset.UtcNow.AddDays(7),
            "superadmin-user",
            DateTimeOffset.UtcNow);

        await using (var firstContext = CreateContext(
            "tenant-one",
            databaseName,
            databaseRoot))
        {
            firstContext.PracticeInvitations.Add(invitation);
            await firstContext.SaveChangesAsync();
        }

        await using var secondContext = CreateContext(
            "tenant-two",
            databaseName,
            databaseRoot);
        var stored = await secondContext.PracticeInvitations.SingleAsync();
        var invitationType = secondContext.Model.FindEntityType(
            typeof(PracticeInvitation));

        Assert.Equal(invitation.Id, stored.Id);
        Assert.Null(invitationType?.FindProperty("TenantId"));
        Assert.NotEqual(
            true,
            invitationType?.FindAnnotation("Finbuckle:MultiTenant")?.Value);
    }

    private static AyoosDbContext CreateContext(
        string tenantIdentifier,
        string databaseName,
        InMemoryDatabaseRoot databaseRoot)
    {
        var options = new DbContextOptionsBuilder<AyoosDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;
        var tenant = new TenantInfo
        {
            Id = Guid.NewGuid().ToString("D"),
            Identifier = tenantIdentifier,
            Name = tenantIdentifier
        };

        return MultiTenantDbContext.Create<AyoosDbContext, TenantInfo>(tenant, options);
    }
}
