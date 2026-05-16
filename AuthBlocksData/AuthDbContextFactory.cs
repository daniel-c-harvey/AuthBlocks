using AuthBlocksData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NetBlocks.Models.Environment;
using NetBlocks.Utilities.Environment;

namespace AuthBlocksData;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();

        var connection = LoadConnection();
        
        optionsBuilder.UseNpgsql(connection.ConnectionString, npgsqlOptions =>
        {
            npgsqlOptions.SetPostgresVersion(18, 3);
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });

        return new AuthDbContext(optionsBuilder.Options);
    }

    private Connection LoadConnection()
    {
        try
        {
            Connections? connections = ConnectionStringTools.LoadFromFile("environment/connections.json");
            if (connections == null) throw new Exception("No connections configuration found");
            Connection? connection = connections.ConnectionStrings
                .FirstOrDefault(c => c.ID == connections.ActiveConnectionID);
            if (connection == null) throw new Exception("Active connection not found");
            return connection;
        }
        catch
        {
            // Design-time / CI: connections.json unavailable; use placeholder.
            // EF bundle does not connect to a database during generation.
            return new Connection { ConnectionString = "Host=localhost;Database=authblocks;Username=skipper" };
        }
    }
}
