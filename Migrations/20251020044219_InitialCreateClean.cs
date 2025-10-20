using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Gaming.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    nombre_real = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    display_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fecha_nacimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    biografia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pais = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    foto_perfil = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    plataforma_preferida = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BibliotecaUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_usuario = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    rawg_game_id = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    game_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    game_image = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Resena = table.Column<string>(type: "text", nullable: false),
                    Calificacion = table.Column<int>(type: "integer", nullable: false),
                    FechaCompletado = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaResena = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioId1 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibliotecaUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BibliotecaUsuario_AspNetUsers_UsuarioId1",
                        column: x => x.UsuarioId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BibliotecaUsuario_AspNetUsers_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Grupos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    foto_grupo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    banner_grupo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
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
                    tipo_archivo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha_subida = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    likes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    love = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    wow = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sad = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    angry = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
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
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibliotecaUsuario_id_usuario_rawg_game_id",
                table: "BibliotecaUsuario",
                columns: new[] { "id_usuario", "rawg_game_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibliotecaUsuario_UsuarioId1",
                table: "BibliotecaUsuario",
                column: "UsuarioId1");

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
                name: "Amigos");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BibliotecaUsuario");

            migrationBuilder.DropTable(
                name: "ComentariosMultimedia");

            migrationBuilder.DropTable(
                name: "ComentariosPublicacion");

            migrationBuilder.DropTable(
                name: "MiembrosGrupo");

            migrationBuilder.DropTable(
                name: "ReaccionesMultimedia");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "PublicacionesGrupo");

            migrationBuilder.DropTable(
                name: "MultimediaGrupo");

            migrationBuilder.DropTable(
                name: "Grupos");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
