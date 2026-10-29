using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HelpdeskSentiment.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CallRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AgentId = table.Column<int>(type: "int", nullable: true),
                    Transcript = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CallTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    SentimentScore = table.Column<double>(type: "float", nullable: false),
                    Sentiment = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallRecords_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Agents",
                columns: new[] { "Id", "Email", "Name" },
                values: new object[,]
                {
                    { 1, "riya@helpdesk.local", "Riya Sharma" },
                    { 2, "aman@helpdesk.local", "Aman Verma" },
                    { 3, "kavya@helpdesk.local", "Kavya Singh" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Email",
                table: "Agents",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_AgentId",
                table: "CallRecords",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_Sentiment",
                table: "CallRecords",
                column: "Sentiment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallRecords");

            migrationBuilder.DropTable(
                name: "Agents");
        }
    }
}
