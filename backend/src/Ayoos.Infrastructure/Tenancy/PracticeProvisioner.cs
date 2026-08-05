using Ayoos.Application.Common.Exceptions;
using Ayoos.Application.Common.Interfaces;
using Ayoos.Application.Common.Security;
using Ayoos.Domain.PracticeInvitations;
using Ayoos.Domain.Practices;
using Ayoos.Infrastructure.Persistence;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ayoos.Infrastructure.Tenancy;

internal sealed class PracticeProvisioner(
    DbContextOptions<AyoosDbContext> dbContextOptions,
    IPracticeInvitationUserService invitationUserService)
    : IPracticeProvisioner
{
    public async Task ProvisionAsync(
        Practice practice,
        Guid invitationId,
        string practiceAdminKeycloakUserId,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default)
    {
        var tenantInfo = new TenantInfo
        {
            Id = practice.Id.ToString("D"),
            Identifier = practice.Slug,
            Name = practice.Name
        };

        try
        {
            await using var dbContext =
                MultiTenantDbContext.Create<AyoosDbContext, TenantInfo>(
                    tenantInfo,
                    dbContextOptions);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "TenantInfo" ("Id", "Identifier", "Name")
                VALUES ({tenantInfo.Id}, {tenantInfo.Identifier}, {tenantInfo.Name})
                """,
                cancellationToken);

            await dbContext.Practices.AddAsync(practice, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var affectedRows = await dbContext.PracticeInvitations
                .Where(invitation =>
                    invitation.Id == invitationId
                    && invitation.Status == PracticeInvitationStatus.Pending
                    && invitation.ExpiresAt > consumedAt)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            invitation => invitation.Status,
                            PracticeInvitationStatus.Consumed)
                        .SetProperty(
                            invitation => invitation.ConsumedAt,
                            consumedAt.ToUniversalTime())
                        .SetProperty(
                            invitation => invitation.PracticeId,
                            practice.Id),
                    cancellationToken);

            PracticeInvitationConsumption.EnsureSucceeded(affectedRows);
            await invitationUserService.AssignPracticeAsync(
                practiceAdminKeycloakUserId,
                practice.Slug,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException(
                $"A practice with slug '{practice.Slug}' already exists.");
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new ConflictException(
                $"A practice with slug '{practice.Slug}' already exists.");
        }
    }
}
