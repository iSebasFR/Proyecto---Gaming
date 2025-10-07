using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewFieldsToBibliotecaUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Calificacion",
                table: "BibliotecaUsuario",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCompletado",
                table: "BibliotecaUsuario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaResena",
                table: "BibliotecaUsuario",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Resena",
                table: "BibliotecaUsuario",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Calificacion",
                table: "BibliotecaUsuario");

            migrationBuilder.DropColumn(
                name: "FechaCompletado",
                table: "BibliotecaUsuario");

            migrationBuilder.DropColumn(
                name: "FechaResena",
                table: "BibliotecaUsuario");

            migrationBuilder.DropColumn(
                name: "Resena",
                table: "BibliotecaUsuario");
        }
    }
}
