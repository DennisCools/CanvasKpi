using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompetenceProfilingInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCriteriaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CriteriaId",
                table: "OutcomesCanvas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriteriaId",
                table: "OutcomesCanvas");
        }
    }
}
