namespace RedAnts.Infrastructure.Ticketing;

public static class SessionCacheSchema
{
    public const string SchemaName = "dbo";
    public const string TableName = "AppSessionCache";

    public const string CreateTableSql = """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppSessionCache' AND schema_id = SCHEMA_ID('dbo'))
        BEGIN
            CREATE TABLE [dbo].[AppSessionCache](
                [Id] nvarchar(449) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL,
                [Value] varbinary(max) NOT NULL,
                [ExpiresAtTime] datetimeoffset(7) NOT NULL,
                [SlidingExpirationInSeconds] bigint NULL,
                [AbsoluteExpiration] datetimeoffset(7) NULL,
                CONSTRAINT [PK_AppSessionCache] PRIMARY KEY CLUSTERED ([Id] ASC)
            );
            CREATE NONCLUSTERED INDEX [Index_AppSessionCache_ExpiresAtTime]
                ON [dbo].[AppSessionCache]([ExpiresAtTime] ASC);
        END
        """;
}
