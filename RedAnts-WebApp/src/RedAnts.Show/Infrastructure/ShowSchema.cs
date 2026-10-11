using NPoco;

namespace RedAnts.Show.Infrastructure;

public static class ShowSchema
{
    public const string SchemaName = "show";

    public const string Ddl = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'show')
            EXEC('CREATE SCHEMA [show]');
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Halls' AND schema_id = SCHEMA_ID('show'))
        BEGIN
            CREATE TABLE [show].[Halls](
                [Id] nvarchar(40) NOT NULL,
                [Json] nvarchar(max) NOT NULL,
                [UpdatedAt] datetime2(0) NOT NULL,
                CONSTRAINT [PK_show_Halls] PRIMARY KEY CLUSTERED ([Id] ASC)
            );
        END
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Games' AND schema_id = SCHEMA_ID('show'))
        BEGIN
            CREATE TABLE [show].[Games](
                [Id] nvarchar(60) NOT NULL,
                [Json] nvarchar(max) NOT NULL,
                [UpdatedAt] datetime2(0) NOT NULL,
                CONSTRAINT [PK_show_Games] PRIMARY KEY CLUSTERED ([Id] ASC)
            );
        END
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Scenes' AND schema_id = SCHEMA_ID('show'))
        BEGIN
            CREATE TABLE [show].[Scenes](
                [Id] nvarchar(60) NOT NULL,
                [SortOrder] int NOT NULL,
                [Json] nvarchar(max) NOT NULL,
                [UpdatedAt] datetime2(0) NOT NULL,
                CONSTRAINT [PK_show_Scenes] PRIMARY KEY CLUSTERED ([Id] ASC)
            );
        END
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Sounds' AND schema_id = SCHEMA_ID('show'))
        BEGIN
            CREATE TABLE [show].[Sounds](
                [Id] nvarchar(60) NOT NULL,
                [SortOrder] int NOT NULL,
                [Json] nvarchar(max) NOT NULL,
                [UpdatedAt] datetime2(0) NOT NULL,
                CONSTRAINT [PK_show_Sounds] PRIMARY KEY CLUSTERED ([Id] ASC)
            );
        END
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AgentReports' AND schema_id = SCHEMA_ID('show'))
        BEGIN
            CREATE TABLE [show].[AgentReports](
                [HallId] nvarchar(40) NOT NULL,
                [Kind] nvarchar(40) NOT NULL,
                [Json] nvarchar(max) NOT NULL,
                [At] datetimeoffset(0) NOT NULL,
                CONSTRAINT [PK_show_AgentReports] PRIMARY KEY CLUSTERED ([HallId] ASC, [Kind] ASC)
            );
        END
        """;

    public static Task EnsureAsync(ShowDatabase database) => database.RunAsync(db => db.ExecuteAsync(Ddl));
}
