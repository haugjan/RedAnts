using Microsoft.Data.SqlClient;

namespace RedAnts.Infrastructure.Show;

public static class ShowSchema
{
    public const string SchemaName = "show";

    public static void Ensure(string connectionString)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'show')
                EXEC('CREATE SCHEMA [show]');

            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SchemaInfo' AND schema_id = SCHEMA_ID('show'))
            BEGIN
                CREATE TABLE [show].[SchemaInfo](
                    [Version] int NOT NULL,
                    [CreatedAt] datetime2(0) NOT NULL,
                    CONSTRAINT [PK_show_SchemaInfo] PRIMARY KEY CLUSTERED ([Version] ASC)
                );
                INSERT INTO [show].[SchemaInfo] ([Version], [CreatedAt]) VALUES (1, SYSUTCDATETIME());
            END
            """;

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }
}
