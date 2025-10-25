using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToRawgApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId",
                table: "BibliotecaUsuario");

            migrationBuilder.DropForeignKey(
                name: "FK_BibliotecaUsuario_Juegos_id_juego",
                table: "BibliotecaUsuario");

            migrationBuilder.DropTable(
                name: "Juegos");

            migrationBuilder.DropIndex(
                name: "IX_BibliotecaUsuario_id_juego",
                table: "BibliotecaUsuario");

            migrationBuilder.DropIndex(
                name: "IX_BibliotecaUsuario_id_usuario",
                table: "BibliotecaUsuario");

            migrationBuilder.RenameColumn(
                name: "id_juego",
                table: "BibliotecaUsuario",
                newName: "rawg_game_id");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "BibliotecaUsuario",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "game_image",
                table: "BibliotecaUsuario",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "game_name",
                table: "BibliotecaUsuario",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_BibliotecaUsuario_id_usuario_rawg_game_id",
                table: "BibliotecaUsuario",
                columns: new[] { "id_usuario", "rawg_game_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibliotecaUsuario_UsuarioId",
                table: "BibliotecaUsuario",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId",
                table: "BibliotecaUsuario",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId",
                table: "BibliotecaUsuario");

            migrationBuilder.DropIndex(
                name: "IX_BibliotecaUsuario_id_usuario_rawg_game_id",
                table: "BibliotecaUsuario");

            migrationBuilder.DropIndex(
                name: "IX_BibliotecaUsuario_UsuarioId",
                table: "BibliotecaUsuario");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "BibliotecaUsuario");

            migrationBuilder.DropColumn(
                name: "game_image",
                table: "BibliotecaUsuario");

            migrationBuilder.DropColumn(
                name: "game_name",
                table: "BibliotecaUsuario");

            migrationBuilder.RenameColumn(
                name: "rawg_game_id",
                table: "BibliotecaUsuario",
                newName: "id_juego");

            migrationBuilder.CreateTable(
                name: "Juegos",
                columns: table => new
                {
                    id_juego = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Categoria = table.Column<string>(type: "text", nullable: false),
                    Imagen = table.Column<string>(type: "text", nullable: true),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Plataforma = table.Column<string>(type: "text", nullable: false),
                    PuntuacionMedia = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Juegos", x => x.id_juego);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BibliotecaUsuario_id_juego",
                table: "BibliotecaUsuario",
                column: "id_juego");

            migrationBuilder.CreateIndex(
                name: "IX_BibliotecaUsuario_id_usuario",
                table: "BibliotecaUsuario",
                column: "id_usuario");

            migrationBuilder.AddForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId",
                table: "BibliotecaUsuario",
                column: "id_usuario",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BibliotecaUsuario_Juegos_id_juego",
                table: "BibliotecaUsuario",
                column: "id_juego",
                principalTable: "Juegos",
                principalColumn: "id_juego",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
