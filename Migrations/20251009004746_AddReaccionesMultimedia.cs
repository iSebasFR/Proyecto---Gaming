using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class AddReaccionesMultimedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "angry",
                table: "MultimediaGrupo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "love",
                table: "MultimediaGrupo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sad",
                table: "MultimediaGrupo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "wow",
                table: "MultimediaGrupo",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReaccionesMultimedia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    multimedia_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    tipo_reaccion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_reaccion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReaccionesMultimedia", x => x.id);
                    table.ForeignKey(
                        name: "FK_ReaccionesMultimedia_AspNetUsers_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReaccionesMultimedia_MultimediaGrupo_multimedia_id",
                        column: x => x.multimedia_id,
                        principalTable: "MultimediaGrupo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReaccionesMultimedia_multimedia_id_usuario_id",
                table: "ReaccionesMultimedia",
                columns: new[] { "multimedia_id", "usuario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReaccionesMultimedia_usuario_id",
                table: "ReaccionesMultimedia",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReaccionesMultimedia");

            migrationBuilder.DropColumn(
                name: "angry",
                table: "MultimediaGrupo");

            migrationBuilder.DropColumn(
                name: "love",
                table: "MultimediaGrupo");

            migrationBuilder.DropColumn(
                name: "sad",
                table: "MultimediaGrupo");

            migrationBuilder.DropColumn(
                name: "wow",
                table: "MultimediaGrupo");
        }
    }
}
