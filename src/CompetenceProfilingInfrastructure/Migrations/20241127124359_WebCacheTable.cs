using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetenceProfilingInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WebCacheTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create WebCache table
            migrationBuilder.Sql("\n CREATE TABLE [dbo].[WebCache](Id nvarchar(449) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL, Value varbinary(MAX) NOT NULL, ExpiresAtTime datetimeoffset NOT NULL,SlidingExpirationInSeconds bigint NULL,AbsoluteExpiration datetimeoffset NULL,PRIMARY KEY (Id)) CREATE NONCLUSTERED INDEX Index_ExpiresAtTime ON [dbo].[WebCache](ExpiresAtTime)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
