using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId1",
                table: "BibliotecaUsuario");

            migrationBuilder.DropIndex(
                name: "IX_BibliotecaUsuario_UsuarioId1",
                table: "BibliotecaUsuario");

            migrationBuilder.DropColumn(
                name: "UsuarioId1",
                table: "BibliotecaUsuario");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "BibliotecaUsuario",
                newName: "id");

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    game_id = table.Column<int>(type: "integer", nullable: false),
                    game_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    payment_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Stripe"),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_Transactions_AspNetUsers_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_usuario_id",
                table: "Transactions",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BibliotecaUsuario",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioId1",
                table: "BibliotecaUsuario",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibliotecaUsuario_UsuarioId1",
                table: "BibliotecaUsuario",
                column: "UsuarioId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId1",
                table: "BibliotecaUsuario",
                column: "UsuarioId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
