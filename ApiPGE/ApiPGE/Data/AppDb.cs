using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Incidencia> Incidencias => Set<Incidencia>();
    public DbSet<Rol> Roles => Set<Rol>(); // ← Nueva
    public DbSet<EstadoIncidencia> EstadosIncidencia => Set<EstadoIncidencia>(); // ← Nueva

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasPostgresExtension("postgis");

        // Elimina las líneas de HasPostgresEnum

        mb.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre");
            e.Property(x => x.Apellido).HasColumnName("apellido");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Username).HasColumnName("username");
            e.Property(x => x.Password).HasColumnName("password");
            e.Property(x => x.RolId).HasColumnName("rol_id"); // ← Cambiado
            e.Property(x => x.Fecha_Alta).HasColumnName("fecha_alta");

            // Relación con Roles
            e.HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.RolId);
        });

        mb.Entity<Rol>(e =>
        {
            e.ToTable("roles", "public");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id"); 
            e.Property(x => x.Nombre).HasColumnName("nombre");
        });


        mb.Entity<EstadoIncidencia>(e =>
        {
            e.ToTable("estado_incidencias", "public"); 
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre");
        });

        mb.Entity<Categoria>(e =>
        {
            e.ToTable("categorias", "public");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre");
        });

        mb.Entity<Incidencia>(e =>
        {
            e.ToTable("incidencias", "public");
            e.HasKey(i => i.Id);

            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.User_Id).HasColumnName("user_id");
            e.Property(i => i.Categoria_Id).HasColumnName("categoria_id");
            e.Property(i => i.Descripcion).HasColumnName("descripcion");
            e.Property(i => i.Foto_Url).HasColumnName("foto_url");
            e.Property(i => i.EstadoId).HasColumnName("estado_id");
            e.Property(i => i.Timestamp).HasColumnName("timestamp");
            e.Property(i => i.Ubicacion).HasColumnName("ubicacion");

            // 🔗 Relaciones explícitas
            e.HasOne(i => i.Usuario)
                .WithMany()
                .HasForeignKey(i => i.User_Id);

            e.HasOne(i => i.Categoria)
                .WithMany()
                .HasForeignKey(i => i.Categoria_Id);

            e.HasOne(i => i.Estado)
                .WithMany()
                .HasForeignKey(i => i.EstadoId);
        });

    }
}