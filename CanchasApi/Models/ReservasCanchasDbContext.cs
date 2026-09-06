using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CanchasApi.Models;

public partial class ReservasCanchasDbContext : DbContext
{
    public ReservasCanchasDbContext()
    {
    }

    public ReservasCanchasDbContext(DbContextOptions<ReservasCanchasDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Usuario> Usuarios { get; set; }
    public virtual DbSet<Trabajador> Trabajadores { get; set; }
    public virtual DbSet<Sede> Sedes { get; set; }
    public virtual DbSet<TipoCancha> TiposCancha { get; set; }
    public virtual DbSet<Cancha> Canchas { get; set; }
    public virtual DbSet<Horario> Horarios { get; set; }
    public virtual DbSet<Reserva> Reservas { get; set; }
    public virtual DbSet<Pago> Pagos { get; set; }
    public virtual DbSet<Comprobante> Comprobantes { get; set; }
    public virtual DbSet<Resena> Resenas { get; set; }
    public virtual DbSet<Mantenimiento> Mantenimientos { get; set; }
    public virtual DbSet<Notificacion> Notificaciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Usuario", tb =>
                tb.HasCheckConstraint("CK_Usuario_Rol", "([Rol]='Cliente' OR [Rol]='Trabajador' OR [Rol]='Administrador')"));

            entity.HasIndex(e => e.Email, "UQ_Usuario_Email").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.Rol).HasMaxLength(20);
        });

        modelBuilder.Entity<Trabajador>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Trabajador");

            entity.HasIndex(e => e.UsuarioId, "UQ_Trabajador_UsuarioId").IsUnique();

            entity.Property(e => e.Cargo).HasMaxLength(50);
            entity.Property(e => e.Turno).HasMaxLength(20);

            entity.HasOne(d => d.Usuario)
                .WithOne(p => p.Trabajador)
                .HasForeignKey<Trabajador>(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Trabajador_Usuario");
        });

        modelBuilder.Entity<Sede>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Sede");

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Direccion).HasMaxLength(200);
            entity.Property(e => e.Telefono).HasMaxLength(20).IsRequired(false);
        });

        modelBuilder.Entity<TipoCancha>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("TipoCancha");

            entity.Property(e => e.Nombre).HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(255).IsRequired(false);
            entity.Property(e => e.PrecioPorHora).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Cancha>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Cancha", tb =>
                tb.HasCheckConstraint("CK_Cancha_Estado", "([Estado]='Fuera de servicio' OR [Estado]='Mantenimiento' OR [Estado]='Disponible')"));

            entity.Property(e => e.Estado).HasMaxLength(20);

            entity.HasOne(d => d.Sede)
                .WithMany(p => p.Canchas)
                .HasForeignKey(d => d.SedeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cancha_Sede");

            entity.HasOne(d => d.TipoCancha)
                .WithMany(p => p.Canchas)
                .HasForeignKey(d => d.TipoCanchaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cancha_TipoCancha");
        });

        modelBuilder.Entity<Horario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Horario", tb =>
            {
                tb.HasCheckConstraint("CK_Horario_Dia", "([DiaSemana]>=(1) AND [DiaSemana]<=(7))");
                tb.HasCheckConstraint("CK_Horario_Rango", "([HoraCierre]>[HoraApertura])");
            });

            entity.HasOne(d => d.Cancha)
                .WithMany(p => p.Horarios)
                .HasForeignKey(d => d.CanchaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Horario_Cancha");
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Reserva", tb =>
            {
                tb.HasCheckConstraint("CK_Reserva_Estado", "([Estado]='Completada' OR [Estado]='Cancelada' OR [Estado]='Confirmada' OR [Estado]='Pendiente')");
                tb.HasCheckConstraint("CK_Reserva_Fechas", "([FechaFin]>[FechaInicio])");
            });

            entity.Property(e => e.Estado).HasMaxLength(20);

            entity.HasOne(d => d.Usuario)
                .WithMany(p => p.Reservas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reserva_Usuario");

            entity.HasOne(d => d.Cancha)
                .WithMany(p => p.Reservas)
                .HasForeignKey(d => d.CanchaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reserva_Cancha");

            entity.HasOne(d => d.Trabajador)
                .WithMany(p => p.Reservas)
                .HasForeignKey(d => d.TrabajadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .IsRequired(false)
                .HasConstraintName("FK_Reserva_Trabajador");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Pago", tb =>
            {
                tb.HasCheckConstraint("CK_Pago_Estado", "([Estado]='Reembolsado' OR [Estado]='Pagado' OR [Estado]='Pendiente')");
                tb.HasCheckConstraint("CK_Pago_Metodo", "([MetodoPago]='Transferencia' OR [MetodoPago]='Tarjeta' OR [MetodoPago]='Efectivo')");
            });

            entity.Property(e => e.Monto).HasPrecision(18, 2);
            entity.Property(e => e.MetodoPago).HasMaxLength(20);
            entity.Property(e => e.Estado).HasMaxLength(20);

            entity.HasOne(d => d.Reserva)
                .WithMany(p => p.Pagos)
                .HasForeignKey(d => d.ReservaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pago_Reserva");
        });

        modelBuilder.Entity<Comprobante>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Comprobante", tb =>
                tb.HasCheckConstraint("CK_Comprobante_Tipo", "([Tipo]='Factura' OR [Tipo]='Boleta')"));

            entity.HasIndex(e => e.PagoId, "UQ_Comprobante_PagoId").IsUnique();

            entity.Property(e => e.NumeroComprobante).HasMaxLength(30);
            entity.Property(e => e.Tipo).HasMaxLength(20);

            entity.HasOne(d => d.Pago)
                .WithOne(p => p.Comprobante)
                .HasForeignKey<Comprobante>(d => d.PagoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Comprobante_Pago");
        });

        modelBuilder.Entity<Resena>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Resena", tb =>
                tb.HasCheckConstraint("CK_Resena_Calificacion", "([Calificacion]>=(1) AND [Calificacion]<=(5))"));

            entity.HasIndex(e => e.ReservaId, "UQ_Resena_ReservaId").IsUnique();

            entity.Property(e => e.Comentario).HasMaxLength(500).IsRequired(false);

            entity.HasOne(d => d.Reserva)
                .WithOne(p => p.Resena)
                .HasForeignKey<Resena>(d => d.ReservaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Resena_Reserva");

            entity.HasOne(d => d.Usuario)
                .WithMany(p => p.Resenas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Resena_Usuario");
        });

        modelBuilder.Entity<Mantenimiento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Mantenimiento");

            entity.Property(e => e.Descripcion).HasMaxLength(300);

            entity.HasOne(d => d.Cancha)
                .WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.CanchaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mantenimiento_Cancha");

            entity.HasOne(d => d.Trabajador)
                .WithMany(p => p.Mantenimientos)
                .HasForeignKey(d => d.TrabajadorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mantenimiento_Trabajador");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Notificacion");

            entity.Property(e => e.Mensaje).HasMaxLength(300);

            entity.HasOne(d => d.Usuario)
                .WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Notificacion_Usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
