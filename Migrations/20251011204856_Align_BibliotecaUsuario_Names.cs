using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class Align_BibliotecaUsuario_Names : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId",
                table: "BibliotecaUsuario");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname='public' AND indexname='IX_BibliotecaUsuario_id_usuario'
    ) THEN
        DROP INDEX public.""IX_BibliotecaUsuario_id_usuario"";
    END IF;
END $$;
");


            migrationBuilder.AlterColumn<string>(
                name: "id_usuario",
                table: "BibliotecaUsuario",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Resena",
                table: "BibliotecaUsuario",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId",
                table: "BibliotecaUsuario",
                column: "id_usuario",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId",
                table: "BibliotecaUsuario");

            migrationBuilder.AlterColumn<string>(
                name: "id_usuario",
                table: "BibliotecaUsuario",
                type: "text",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Resena",
                table: "BibliotecaUsuario",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibliotecaUsuario_id_usuario",
                table: "BibliotecaUsuario",
                column: "id_usuario");

            migrationBuilder.AddForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId",
                table: "BibliotecaUsuario",
                column: "id_usuario",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
