using Microsoft.EntityFrameworkCore;

namespace Ayoos.Infrastructure.Persistence;

public sealed class AyoosDbContext(DbContextOptions<AyoosDbContext> options)
    : DbContext(options);
