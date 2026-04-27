using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 🔹 DbSets
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<SolicitudCredito> Solicitudes { get; set; }

        // 🔹 Configuración del modelo
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Relación Cliente → Solicitudes
            builder.Entity<Cliente>()
                .HasMany(c => c.Solicitudes)
                .WithOne(s => s.Cliente)
                .HasForeignKey(s => s.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🚨 REGLA CLAVE: solo una solicitud pendiente por cliente
            builder.Entity<SolicitudCredito>()
                .HasIndex(s => new { s.ClienteId, s.Estado })
                .HasFilter("Estado = 0") // 0 = Pendiente
                .IsUnique();
        }
    }
}