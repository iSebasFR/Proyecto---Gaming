using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Models.Comunidad;
using Proyecto_Gaming.Models.Payment;

namespace Proyecto_Gaming.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Biblioteca existente
        public DbSet<BibliotecaUsuario> BibliotecaUsuario { get; set; }
        
        // Modelos de Comunidad
        public DbSet<Amigo> Amigos { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<MiembroGrupo> MiembrosGrupo { get; set; }
        public DbSet<PublicacionGrupo> PublicacionesGrupo { get; set; }
        public DbSet<ComentarioPublicacion> ComentariosPublicacion { get; set; }
        public DbSet<MultimediaGrupo> MultimediaGrupo { get; set; }
        public DbSet<ComentarioMultimedia> ComentariosMultimedia { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        
        // NUEVO: DbSet para reacciones de multimedia
        public DbSet<ReaccionMultimedia> ReaccionesMultimedia { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración existente para BibliotecaUsuario
            modelBuilder.Entity<BibliotecaUsuario>(entity =>
            {
                entity.ToTable("BibliotecaUsuario");
                entity.HasKey(bu => bu.Id);
                
                entity.Property(bu => bu.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                    
                entity.Property(bu => bu.UsuarioId)
                    .HasColumnName("id_usuario")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(bu => bu.RawgGameId)
                    .HasColumnName("rawg_game_id")
                    .IsRequired();

                entity.Property(bu => bu.Estado)
                    .HasColumnName("estado")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(bu => bu.GameName)
                    .HasColumnName("game_name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(bu => bu.GameImage)
                    .HasColumnName("game_image")
                    .HasMaxLength(500);

                // Índice único para evitar duplicados
                entity.HasIndex(bu => new { bu.UsuarioId, bu.RawgGameId })
                    .IsUnique();
            });

            // Configuración existente para Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.Property(u => u.NombreReal)
                    .HasColumnName("nombre_real")
                    .HasMaxLength(255);

                entity.Property(u => u.DisplayName)
                    .HasColumnName("display_name")
                    .HasMaxLength(50);

                entity.Property(u => u.FechaNacimiento)
                    .HasColumnName("fecha_nacimiento");

                entity.Property(u => u.Biografia)
                    .HasColumnName("biografia")
                    .HasMaxLength(500);

                entity.Property(u => u.Pais)
                    .HasColumnName("pais")
                    .HasMaxLength(100);

                entity.Property(u => u.FotoPerfil)
                    .HasColumnName("foto_perfil")
                    .HasMaxLength(500);

                entity.Property(u => u.PlataformaPreferida)
                    .HasColumnName("plataforma_preferida")
                    .HasMaxLength(200);

                entity.Property(u => u.FechaRegistro)
                    .HasColumnName("fecha_registro")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(u => u.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(50);
            });

            // Configuración para Amigos
            modelBuilder.Entity<Amigo>(entity =>
            {
                entity.ToTable("Amigos");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(a => a.UsuarioId)
                    .HasColumnName("usuario_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(a => a.AmigoId)
                    .HasColumnName("amigo_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(a => a.Estado)
                    .HasColumnName("estado")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(a => a.FechaSolicitud)
                    .HasColumnName("fecha_solicitud")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(a => a.FechaAceptacion)
                    .HasColumnName("fecha_aceptacion");

                // Relaciones
                entity.HasOne(a => a.Usuario)
                    .WithMany()
                    .HasForeignKey(a => a.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.AmigoUsuario)
                    .WithMany()
                    .HasForeignKey(a => a.AmigoId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índice único para evitar duplicados
                entity.HasIndex(a => new { a.UsuarioId, a.AmigoId })
                    .IsUnique();
            });

            // Configuración para Grupos
            modelBuilder.Entity<Grupo>(entity =>
            {
                entity.ToTable("Grupos");
                entity.HasKey(g => g.Id);

                entity.Property(g => g.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(g => g.Nombre)
                    .HasColumnName("nombre")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(g => g.Descripcion)
                    .HasColumnName("descripcion")
                    .HasMaxLength(500);

                entity.Property(g => g.FotoGrupo)
                    .HasColumnName("foto_grupo")
                    .HasMaxLength(255);

                entity.Property(g => g.BannerGrupo)
                    .HasColumnName("banner_grupo")
                    .HasMaxLength(255);

                entity.Property(g => g.Categoria)
                    .HasColumnName("categoria")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(g => g.CreadorId)
                    .HasColumnName("creador_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(g => g.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(g => g.EsPublico)
                    .HasColumnName("es_publico")
                    .HasDefaultValue(true);

                // Relación con el creador
                entity.HasOne(g => g.Creador)
                    .WithMany()
                    .HasForeignKey(g => g.CreadorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración para MiembrosGrupo
            modelBuilder.Entity<MiembroGrupo>(entity =>
            {
                entity.ToTable("MiembrosGrupo");
                entity.HasKey(mg => mg.Id);

                entity.Property(mg => mg.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(mg => mg.GrupoId)
                    .HasColumnName("grupo_id")
                    .IsRequired();

                entity.Property(mg => mg.UsuarioId)
                    .HasColumnName("usuario_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(mg => mg.Rol)
                    .HasColumnName("rol")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(mg => mg.FechaUnion)
                    .HasColumnName("fecha_union")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relaciones
                entity.HasOne(mg => mg.Grupo)
                    .WithMany(g => g.Miembros)
                    .HasForeignKey(mg => mg.GrupoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mg => mg.Usuario)
                    .WithMany()
                    .HasForeignKey(mg => mg.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índice único para evitar miembros duplicados
                entity.HasIndex(mg => new { mg.GrupoId, mg.UsuarioId })
                    .IsUnique();
            });

            // Configuración para PublicacionesGrupo
            modelBuilder.Entity<PublicacionGrupo>(entity =>
            {
                entity.ToTable("PublicacionesGrupo");
                entity.HasKey(pg => pg.Id);

                entity.Property(pg => pg.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(pg => pg.GrupoId)
                    .HasColumnName("grupo_id")
                    .IsRequired();

                entity.Property(pg => pg.UsuarioId)
                    .HasColumnName("usuario_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(pg => pg.Contenido)
                    .HasColumnName("contenido")
                    .IsRequired();

                entity.Property(pg => pg.FechaPublicacion)
                    .HasColumnName("fecha_publicacion")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(pg => pg.FechaEdicion)
                    .HasColumnName("fecha_edicion");

                entity.Property(pg => pg.Likes)
                    .HasColumnName("likes")
                    .HasDefaultValue(0);

                // Relaciones
                entity.HasOne(pg => pg.Grupo)
                    .WithMany(g => g.Publicaciones)
                    .HasForeignKey(pg => pg.GrupoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pg => pg.Usuario)
                    .WithMany()
                    .HasForeignKey(pg => pg.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para ComentariosPublicacion
            modelBuilder.Entity<ComentarioPublicacion>(entity =>
            {
                entity.ToTable("ComentariosPublicacion");
                entity.HasKey(cp => cp.Id);

                entity.Property(cp => cp.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(cp => cp.PublicacionId)
                    .HasColumnName("publicacion_id")
                    .IsRequired();

                entity.Property(cp => cp.UsuarioId)
                    .HasColumnName("usuario_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(cp => cp.Contenido)
                    .HasColumnName("contenido")
                    .IsRequired();

                entity.Property(cp => cp.FechaComentario)
                    .HasColumnName("fecha_comentario")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relaciones
                entity.HasOne(cp => cp.Publicacion)
                    .WithMany(p => p.Comentarios)
                    .HasForeignKey(cp => cp.PublicacionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cp => cp.Usuario)
                    .WithMany()
                    .HasForeignKey(cp => cp.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para MultimediaGrupo
            modelBuilder.Entity<MultimediaGrupo>(entity =>
            {
                entity.ToTable("MultimediaGrupo");
                entity.HasKey(mg => mg.Id);

                entity.Property(mg => mg.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(mg => mg.GrupoId)
                    .HasColumnName("grupo_id")
                    .IsRequired();

                entity.Property(mg => mg.UsuarioId)
                    .HasColumnName("usuario_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(mg => mg.UrlArchivo)
                    .HasColumnName("url_archivo")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(mg => mg.TipoArchivo)
                    .HasColumnName("tipo_archivo")
                    .HasMaxLength(50);

                entity.Property(mg => mg.Descripcion)
                    .HasColumnName("descripcion")
                    .HasMaxLength(200);

                entity.Property(mg => mg.FechaSubida)
                    .HasColumnName("fecha_subida")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(mg => mg.Likes)
                    .HasColumnName("likes")
                    .HasDefaultValue(0);

                // Nuevos campos para reacciones
                entity.Property(mg => mg.Love)
                    .HasColumnName("love")
                    .HasDefaultValue(0);

                entity.Property(mg => mg.Wow)
                    .HasColumnName("wow")
                    .HasDefaultValue(0);

                entity.Property(mg => mg.Sad)
                    .HasColumnName("sad")
                    .HasDefaultValue(0);

                entity.Property(mg => mg.Angry)
                    .HasColumnName("angry")
                    .HasDefaultValue(0);

                // Relaciones
                entity.HasOne(mg => mg.Grupo)
                    .WithMany(g => g.Multimedia)
                    .HasForeignKey(mg => mg.GrupoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mg => mg.Usuario)
                    .WithMany()
                    .HasForeignKey(mg => mg.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para ComentariosMultimedia
            modelBuilder.Entity<ComentarioMultimedia>(entity =>
            {
                entity.ToTable("ComentariosMultimedia");
                entity.HasKey(cm => cm.Id);

                entity.Property(cm => cm.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(cm => cm.MultimediaId)
                    .HasColumnName("multimedia_id")
                    .IsRequired();

                entity.Property(cm => cm.UsuarioId)
                    .HasColumnName("usuario_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(cm => cm.Contenido)
                    .HasColumnName("contenido")
                    .IsRequired();

                entity.Property(cm => cm.FechaComentario)
                    .HasColumnName("fecha_comentario")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relaciones
                entity.HasOne(cm => cm.Multimedia)
                    .WithMany(m => m.Comentarios)
                    .HasForeignKey(cm => cm.MultimediaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cm => cm.Usuario)
                    .WithMany()
                    .HasForeignKey(cm => cm.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // NUEVO: Configuración para ReaccionesMultimedia
            modelBuilder.Entity<ReaccionMultimedia>(entity =>
            {
                entity.ToTable("ReaccionesMultimedia");
                entity.HasKey(rm => rm.Id);

                entity.Property(rm => rm.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(rm => rm.MultimediaId)
                    .HasColumnName("multimedia_id")
                    .IsRequired();

                entity.Property(rm => rm.UsuarioId)
                    .HasColumnName("usuario_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(rm => rm.TipoReaccion)
                    .HasColumnName("tipo_reaccion")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(rm => rm.FechaReaccion)
                    .HasColumnName("fecha_reaccion")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relaciones
                entity.HasOne(rm => rm.Multimedia)
                    .WithMany(m => m.Reacciones)
                    .HasForeignKey(rm => rm.MultimediaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rm => rm.Usuario)
                    .WithMany()
                    .HasForeignKey(rm => rm.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índice único para evitar reacciones duplicadas del mismo usuario
                entity.HasIndex(rm => new { rm.MultimediaId, rm.UsuarioId })
                    .IsUnique();
            });


            // Configuración para Transactions
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(t => t.UsuarioId)
                    .HasColumnName("usuario_id")
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(t => t.GameId)
                    .HasColumnName("game_id")
                    .IsRequired();

                entity.Property(t => t.GameTitle)
                    .HasColumnName("game_title")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(t => t.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(t => t.Currency)
                    .HasColumnName("currency")
                    .HasMaxLength(10)
                    .HasDefaultValue("USD");

                entity.Property(t => t.PaymentStatus)
                    .HasColumnName("payment_status")
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");

                entity.Property(t => t.PaymentProvider)
                    .HasColumnName("payment_provider")
                    .HasMaxLength(50)
                    .HasDefaultValue("Stripe");

                entity.Property(t => t.TransactionId)
                    .HasColumnName("transaction_id")
                    .HasMaxLength(100);

                entity.Property(t => t.SessionId)
                    .HasColumnName("session_id")
                    .HasMaxLength(100);

                entity.Property(t => t.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(t => t.CompletedAt)
                    .HasColumnName("completed_at");

                // Relación con Usuario
                entity.HasOne(t => t.Usuario)
                    .WithMany()
                    .HasForeignKey(t => t.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}