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

        // SOLO estos DbSets
        public DbSet<Juego> Juegos { get; set; }
        public DbSet<BibliotecaUsuario> BibliotecaUsuario { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // IMPORTANTE para Identity

            // Configuración de Juegos (tu catálogo)
            modelBuilder.Entity<Juego>(entity =>
            {
                entity.ToTable("Juegos");
                entity.HasKey(j => j.IdJuego);
                entity.Property(j => j.IdJuego)
                    .HasColumnName("id_juego")
                    .ValueGeneratedOnAdd();
                entity.Property(j => j.Nombre)
                    .HasColumnName("nombre")
                    .IsRequired()
                    .HasMaxLength(255);
                // ... resto de configuración de Juegos
            });

            // Configuración de BibliotecaUsuario
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
                    .HasMaxLength(450); // Tamaño para IDs de Identity

                entity.Property(bu => bu.IdJuego)
                    .HasColumnName("id_juego")
                    .IsRequired();

                entity.Property(bu => bu.Estado)
                    .HasColumnName("estado")
                    .IsRequired()
                    .HasMaxLength(50);

                // Relaciones
                entity.HasOne(bu => bu.Usuario)
                    .WithMany(u => u.BibliotecaUsuarios)
                    .HasForeignKey(bu => bu.IdUsuario)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bu => bu.Juego)
                    .WithMany(j => j.BibliotecaUsuarios)
                    .HasForeignKey(bu => bu.IdJuego)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de propiedades personalizadas de Usuario
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