using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<CodigoSequence> CodigoSequences => Set<CodigoSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(v => v.Ciudad).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PrecioEntrada).HasPrecision(18, 2);
            entity.Property(e => e.Tipo).HasConversion<string>();
            entity.Property(e => e.Estado).HasConversion<string>();
            entity.HasOne(e => e.Venue).WithMany(v => v.Eventos).HasForeignKey(e => e.VenueId);
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.NombreComprador).HasMaxLength(200).IsRequired();
            entity.Property(r => r.EmailComprador).HasMaxLength(200).IsRequired();
            entity.Property(r => r.CodigoReserva).HasMaxLength(20);
            entity.Property(r => r.Estado).HasConversion<string>();
            entity.HasOne(r => r.Evento).WithMany(e => e.Reservas).HasForeignKey(r => r.EventoId);
            entity.HasIndex(r => r.CodigoReserva).IsUnique();
        });

        modelBuilder.Entity<CodigoSequence>(entity =>
        {
            entity.HasKey(c => c.Id);
        });

        SeedVenues(modelBuilder);
    }

    private static void SeedVenues(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>().HasData(
            new Venue { Id = 1, Nombre = "Auditorio Central", Capacidad = 200, Ciudad = "Bogotá" },
            new Venue { Id = 2, Nombre = "Sala Norte", Capacidad = 50, Ciudad = "Bogotá" },
            new Venue { Id = 3, Nombre = "Arena Sur", Capacidad = 500, Ciudad = "Medellín" });

        modelBuilder.Entity<CodigoSequence>().HasData(new CodigoSequence { Id = 1, LastValue = 0 });
    }
}

public class CodigoSequence
{
    public int Id { get; set; }
    public int LastValue { get; set; }
}
