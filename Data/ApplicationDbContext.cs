using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // SOLO BibliotecaUsuario - ELIMINAR Juegos ya que usamos RAWG API
        public DbSet<BibliotecaUsuario> BibliotecaUsuario { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // IMPORTANTE para Identity

            // Configuración para la tabla BibliotecaUsuario (ACTUALIZADA PARA RAWG)
            modelBuilder.Entity<BibliotecaUsuario>(entity =>
            {
                entity.ToTable("BibliotecaUsuario");
                entity.HasKey(bu => bu.Id);
                
                entity.Property(bu => bu.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                    
                entity.Property(bu => bu.IdUsuario)
                    .HasColumnName("id_usuario")
                    .IsRequired()
                    .HasMaxLength(450);

                // NUEVAS PROPIEDADES PARA RAWG API
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

                // Índice único para evitar duplicados (mismo usuario + mismo juego RAWG)
                entity.HasIndex(bu => new { bu.IdUsuario, bu.RawgGameId })
                    .IsUnique();
            });

            // Configuración adicional para el modelo Usuario de Identity
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.Property(u => u.NombreReal)
                    .HasColumnName("nombre_real")
                    .HasMaxLength(255);

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
                    .HasMaxLength(50);

                entity.Property(u => u.FechaRegistro)
                    .HasColumnName("fecha_registro")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(u => u.Estado)
                    .HasColumnName("estado")
                    .HasMaxLength(50);
            });
        }
    }
}