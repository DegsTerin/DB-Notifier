// Module purpose: Implements Server Db Context Factory for central PostgreSQL persistence with transactional and authorisation boundaries.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DBNotifier.Persistence.Server.PostgreSql;

public sealed class ServerDbContextFactory : IDesignTimeDbContextFactory<ServerDbContext>
{
    public ServerDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ServerDbContext> options = new();
        options.UseNpgsql();
        return new ServerDbContext(options.Options);
    }
}
