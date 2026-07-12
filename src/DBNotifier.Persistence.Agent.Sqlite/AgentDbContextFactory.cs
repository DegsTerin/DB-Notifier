// Module purpose: Implements Agent Db Context Factory for the Agent-local SQLite boundary without exposing monitored database secrets.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed class AgentDbContextFactory : IDesignTimeDbContextFactory<AgentDbContext>
{
    public AgentDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<AgentDbContext> options = new();
        options.UseSqlite();
        return new AgentDbContext(options.Options);
    }
}
