using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CalorieTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OriginalQuery = table.Column<string>(type: "text", nullable: false),
                    FoodName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Portion = table.Column<string>(type: "text", nullable: false),
                    Calories = table.Column<double>(type: "double precision", nullable: false),
                    ProteinGrams = table.Column<double>(type: "double precision", nullable: false),
                    CarbsGrams = table.Column<double>(type: "double precision", nullable: false),
                    FatGrams = table.Column<double>(type: "double precision", nullable: false),
                    MealType = table.Column<int>(type: "integer", nullable: false),
                    IsFound = table.Column<bool>(type: "boolean", nullable: false),
                    ConsumedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_UserId_ConsumedDate",
                table: "FoodItems",
                columns: new[] { "UserId", "ConsumedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodItems");
        }
    }
}
