using Examen_Parcial.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Examen_Parcial.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<SolicitudCredito> SolicitudesCredito { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Cliente>()
                .Property(c => c.IngresosMensuales)
                .HasConversion<double>();

            builder.Entity<SolicitudCredito>()
                .Property(s => s.MontoSolicitado)
                .HasConversion<double>();
        }
    }
}