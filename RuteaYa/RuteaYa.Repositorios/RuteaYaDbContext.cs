using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
namespace RuteaYa.Repositorios
{
    public class RuteaYaDbContext : DbContext
    {
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Viaje> Viajes { get; set; }
        public DbSet<Envio> Envios { get; set; }
        public DbSet<Parada> Paradas { get; set; }
        public DbSet<ViajeDia> ViajeDias { get; set; }
        public DbSet<Asignacion> Asignaciones { get; set; }
        public DbSet<EnvioEstadoHistorial> EnvioEstadoHistoriales { get; set; }

        public RuteaYaDbContext(DbContextOptions<RuteaYaDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //aplica la config automatica por defecto de EF
            base.OnModelCreating(modelBuilder);

            // 1. Relación 1 a N: Un Usuario publica muchos Envios
            modelBuilder.Entity<Envio>()
                .HasOne(e => e.Usuario)
                //vinculado a la coleccion dentro de usuario
                .WithMany(u => u.Envios)
                .HasForeignKey(e => e.UsuarioId)
                //Impide borrar Usuarios si hay envios vinculados
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Relación 1 a N: Un Usuario crea muchos Viajes
            modelBuilder.Entity<Viaje>()
                .HasOne(v => v.Usuario)
                .WithMany(u => u.Viajes)
                .HasForeignKey(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Relación 1 a N: Un Viaje tiene muchas Paradas
            modelBuilder.Entity<Parada>()
                .HasOne(p => p.Viaje)
                .WithMany(v => v.Paradas)
                .HasForeignKey(p => p.ViajeId)
                .OnDelete(DeleteBehavior.Cascade);

            // 4. Configuración de Clave Compuesta para ViajeDia (ViajeId + DiaSemana)
            modelBuilder.Entity<ViajeDia>()
                .HasKey(vd => new { vd.ViajeId, vd.DiaSemana });

            modelBuilder.Entity<ViajeDia>()
                .HasOne(vd => vd.Viaje)
                .WithMany(v => v.ViajeDias)
                .HasForeignKey(vd => vd.ViajeId)
                .OnDelete(DeleteBehavior.Cascade);

            // 5. Relaciones de Asignacion (Envio e Viaje)
            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Envio)
                .WithMany()
                .HasForeignKey(a => a.EnvioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Viaje)
                .WithMany(v => v.Asignaciones)
                .HasForeignKey(a => a.ViajeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 6. Relación 1 a N: Un Envio tiene muchos registros en su Historial de Estados
            modelBuilder.Entity<EnvioEstadoHistorial>()
                .HasOne(h => h.Envio)
                .WithMany()
                .HasForeignKey(h => h.EnvioId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
