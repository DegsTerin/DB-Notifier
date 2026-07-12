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
