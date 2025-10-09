using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class AddComunidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Amigos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    amigo_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_solicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    fecha_aceptacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amigos", x => x.id);
                    table.ForeignKey(
                        name: "FK_Amigos_AspNetUsers_amigo_id",
                        column: x => x.amigo_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Amigos_AspNetUsers_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Grupos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    foto_grupo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    banner_grupo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    creador_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    es_publico = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupos", x => x.id);
                    table.ForeignKey(
                        name: "FK_Grupos_AspNetUsers_creador_id",
                        column: x => x.creador_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MiembrosGrupo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grupo_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_union = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiembrosGrupo", x => x.id);
                    table.ForeignKey(
                        name: "FK_MiembrosGrupo_AspNetUsers_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MiembrosGrupo_Grupos_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "Grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MultimediaGrupo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grupo_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    url_archivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo_archivo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fecha_subida = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    likes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultimediaGrupo", x => x.id);
                    table.ForeignKey(
                        name: "FK_MultimediaGrupo_AspNetUsers_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultimediaGrupo_Grupos_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "Grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicacionesGrupo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grupo_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false),
                    fecha_publicacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    fecha_edicion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    likes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicacionesGrupo", x => x.id);
                    table.ForeignKey(
                        name: "FK_PublicacionesGrupo_AspNetUsers_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicacionesGrupo_Grupos_grupo_id",
                        column: x => x.grupo_id,
                        principalTable: "Grupos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComentariosMultimedia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    multimedia_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false),
                    fecha_comentario = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComentariosMultimedia", x => x.id);
                    table.ForeignKey(
                        name: "FK_ComentariosMultimedia_AspNetUsers_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComentariosMultimedia_MultimediaGrupo_multimedia_id",
                        column: x => x.multimedia_id,
                        principalTable: "MultimediaGrupo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComentariosPublicacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    publicacion_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false),
                    fecha_comentario = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComentariosPublicacion", x => x.id);
                    table.ForeignKey(
                        name: "FK_ComentariosPublicacion_AspNetUsers_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComentariosPublicacion_PublicacionesGrupo_publicacion_id",
                        column: x => x.publicacion_id,
                        principalTable: "PublicacionesGrupo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Amigos_amigo_id",
                table: "Amigos",
                column: "amigo_id");

            migrationBuilder.CreateIndex(
                name: "IX_Amigos_usuario_id_amigo_id",
                table: "Amigos",
                columns: new[] { "usuario_id", "amigo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosMultimedia_multimedia_id",
                table: "ComentariosMultimedia",
                column: "multimedia_id");

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosMultimedia_usuario_id",
                table: "ComentariosMultimedia",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosPublicacion_publicacion_id",
                table: "ComentariosPublicacion",
                column: "publicacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosPublicacion_usuario_id",
                table: "ComentariosPublicacion",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_creador_id",
                table: "Grupos",
                column: "creador_id");

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosGrupo_grupo_id_usuario_id",
                table: "MiembrosGrupo",
                columns: new[] { "grupo_id", "usuario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MiembrosGrupo_usuario_id",
                table: "MiembrosGrupo",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_MultimediaGrupo_grupo_id",
                table: "MultimediaGrupo",
                column: "grupo_id");

            migrationBuilder.CreateIndex(
                name: "IX_MultimediaGrupo_usuario_id",
                table: "MultimediaGrupo",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_PublicacionesGrupo_grupo_id",
                table: "PublicacionesGrupo",
                column: "grupo_id");

            migrationBuilder.CreateIndex(
                name: "IX_PublicacionesGrupo_usuario_id",
                table: "PublicacionesGrupo",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Amigos");

            migrationBuilder.DropTable(
                name: "ComentariosMultimedia");

            migrationBuilder.DropTable(
                name: "ComentariosPublicacion");

            migrationBuilder.DropTable(
                name: "MiembrosGrupo");

            migrationBuilder.DropTable(
                name: "MultimediaGrupo");

            migrationBuilder.DropTable(
                name: "PublicacionesGrupo");

            migrationBuilder.DropTable(
                name: "Grupos");
        }
    }
}
