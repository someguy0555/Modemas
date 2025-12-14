using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modemas.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionTopicGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Topic = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionTopicGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    TimeLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionTopicGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    QuestionType = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    MultipleAnswerQuestion_Choices = table.Column<string>(type: "TEXT", nullable: true),
                    CorrectAnswerIndices = table.Column<string>(type: "TEXT", nullable: true),
                    Choices = table.Column<string>(type: "TEXT", nullable: true),
                    CorrectAnswerIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    CorrectAnswer = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_QuestionTopicGroups_QuestionTopicGroupId",
                        column: x => x.QuestionTopicGroupId,
                        principalTable: "QuestionTopicGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionTopicGroupId",
                table: "Questions",
                column: "QuestionTopicGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTopicGroups_Topic",
                table: "QuestionTopicGroups",
                column: "Topic",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "QuestionTopicGroups");
        }
    }
}
