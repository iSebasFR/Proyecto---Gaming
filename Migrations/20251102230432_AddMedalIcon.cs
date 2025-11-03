using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class AddMedalIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveyAnswers_SurveyOptions_OptionId",
                table: "SurveyAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyAnswers_SurveyQuestions_QuestionId",
                table: "SurveyAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyResponses_AspNetUsers_UsuarioId",
                table: "SurveyResponses");

            migrationBuilder.RenameColumn(
                name: "TextAnswer",
                table: "SurveyAnswers",
                newName: "AnswerText");

            migrationBuilder.RenameColumn(
                name: "QuestionId",
                table: "SurveyAnswers",
                newName: "SurveyQuestionId");

            migrationBuilder.RenameColumn(
                name: "OptionId",
                table: "SurveyAnswers",
                newName: "SelectedOptionId");

            migrationBuilder.RenameIndex(
                name: "IX_SurveyAnswers_QuestionId",
                table: "SurveyAnswers",
                newName: "IX_SurveyAnswers_SurveyQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_SurveyAnswers_OptionId",
                table: "SurveyAnswers",
                newName: "IX_SurveyAnswers_SelectedOptionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDateUtc",
                table: "Surveys",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "SurveyResponses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "UserMedals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    medal_id = table.Column<int>(type: "integer", nullable: false),
                    granted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMedals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMedals_AspNetUsers_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserMedals_Medals_medal_id",
                        column: x => x.medal_id,
                        principalTable: "Medals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMedals_medal_id",
                table: "UserMedals",
                column: "medal_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserMedals_usuario_id_medal_id",
                table: "UserMedals",
                columns: new[] { "usuario_id", "medal_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyAnswers_SurveyOptions_SelectedOptionId",
                table: "SurveyAnswers",
                column: "SelectedOptionId",
                principalTable: "SurveyOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyAnswers_SurveyQuestions_SurveyQuestionId",
                table: "SurveyAnswers",
                column: "SurveyQuestionId",
                principalTable: "SurveyQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyResponses_AspNetUsers_UsuarioId",
                table: "SurveyResponses",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveyAnswers_SurveyOptions_SelectedOptionId",
                table: "SurveyAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyAnswers_SurveyQuestions_SurveyQuestionId",
                table: "SurveyAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyResponses_AspNetUsers_UsuarioId",
                table: "SurveyResponses");

            migrationBuilder.DropTable(
                name: "UserMedals");

            migrationBuilder.RenameColumn(
                name: "SurveyQuestionId",
                table: "SurveyAnswers",
                newName: "QuestionId");

            migrationBuilder.RenameColumn(
                name: "SelectedOptionId",
                table: "SurveyAnswers",
                newName: "OptionId");

            migrationBuilder.RenameColumn(
                name: "AnswerText",
                table: "SurveyAnswers",
                newName: "TextAnswer");

            migrationBuilder.RenameIndex(
                name: "IX_SurveyAnswers_SurveyQuestionId",
                table: "SurveyAnswers",
                newName: "IX_SurveyAnswers_QuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_SurveyAnswers_SelectedOptionId",
                table: "SurveyAnswers",
                newName: "IX_SurveyAnswers_OptionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDateUtc",
                table: "Surveys",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "SurveyResponses",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyAnswers_SurveyOptions_OptionId",
                table: "SurveyAnswers",
                column: "OptionId",
                principalTable: "SurveyOptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyAnswers_SurveyQuestions_QuestionId",
                table: "SurveyAnswers",
                column: "QuestionId",
                principalTable: "SurveyQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyResponses_AspNetUsers_UsuarioId",
                table: "SurveyResponses",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
