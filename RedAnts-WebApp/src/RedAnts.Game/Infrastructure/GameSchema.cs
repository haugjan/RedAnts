using NPoco;

namespace RedAnts.Game.Infrastructure;

public static class GameSchema
{
    public const string SchemaName = "game";

    public const string Ddl = """
        IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'game')
            EXEC('CREATE SCHEMA [game]');
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SchemaInfo' AND schema_id = SCHEMA_ID('game'))
        BEGIN
            CREATE TABLE [game].[SchemaInfo](
                [Version] int NOT NULL,
                [CreatedAt] datetimeoffset NOT NULL,
                CONSTRAINT [PK_game_SchemaInfo] PRIMARY KEY CLUSTERED ([Version] ASC)
            );
            INSERT INTO [game].[SchemaInfo] ([Version], [CreatedAt]) VALUES (1, SYSDATETIMEOFFSET());
        END
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Players' AND schema_id = SCHEMA_ID('game'))
        BEGIN
            CREATE TABLE [game].[Players](
                [ExternalId] int NOT NULL,
                [Name] nvarchar(120) NOT NULL,
                [Position] int NOT NULL,
                [Club] nvarchar(120) NOT NULL,
                [Number] nvarchar(8) NOT NULL,
                [BirthYear] int NULL,
                [Height] nvarchar(16) NULL,
                [PortraitUrl] nvarchar(500) NULL,
                [MarketValue] int NOT NULL,
                [Games] int NOT NULL,
                [Goals] int NOT NULL,
                [Assists] int NOT NULL,
                [BestPlayer] int NOT NULL,
                [PenaltyMinutes] int NOT NULL,
                [GoalsAgainstPerGame] decimal(6,2) NOT NULL,
                [RawTotal] int NOT NULL,
                [RawAverage] decimal(6,2) NOT NULL,
                [PeakPoints] int NOT NULL,
                [PeakGame] int NOT NULL,
                [RecentAverage] decimal(6,2) NOT NULL,
                [Status] int NOT NULL,
                [Licence] nvarchar(160) NULL,
                [FormCsv] nvarchar(max) NOT NULL,
                [PointsJson] nvarchar(max) NULL,
                [Season] nvarchar(20) NOT NULL,
                [UpdatedAt] datetimeoffset NOT NULL,
                CONSTRAINT [PK_game_Players] PRIMARY KEY CLUSTERED ([ExternalId] ASC)
            );
            CREATE INDEX [IX_game_Players_Position_MarketValue]
                ON [game].[Players]([Position] ASC, [MarketValue] DESC);
        END
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Squads' AND schema_id = SCHEMA_ID('game'))
        BEGIN
            CREATE TABLE [game].[Squads](
                [ManagerToken] nvarchar(64) NOT NULL,
                [Name] nvarchar(60) NOT NULL,
                [CreatedAt] datetimeoffset NOT NULL,
                [UpdatedAt] datetimeoffset NOT NULL,
                CONSTRAINT [PK_game_Squads] PRIMARY KEY CLUSTERED ([ManagerToken] ASC)
            );
        END
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SquadSlots' AND schema_id = SCHEMA_ID('game'))
        BEGIN
            CREATE TABLE [game].[SquadSlots](
                [ManagerToken] nvarchar(64) NOT NULL,
                [Slot] int NOT NULL,
                [PlayerId] int NOT NULL,
                [MarketValue] int NOT NULL,
                CONSTRAINT [PK_game_SquadSlots] PRIMARY KEY CLUSTERED ([ManagerToken] ASC, [Slot] ASC)
            );
        END
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Settings' AND schema_id = SCHEMA_ID('game'))
        BEGIN
            CREATE TABLE [game].[Settings](
                [Key] nvarchar(100) NOT NULL,
                [Value] nvarchar(max) NULL,
                [UpdatedAt] datetimeoffset NOT NULL,
                CONSTRAINT [PK_game_Settings] PRIMARY KEY CLUSTERED ([Key] ASC)
            );
        END
        """;

    public static Task EnsureAsync(GameDatabase database) => database.RunAsync(db => db.ExecuteAsync(Ddl));
}
