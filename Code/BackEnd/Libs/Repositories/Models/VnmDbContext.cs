using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Models;

public partial class VnmDbContext : DbContext
{
    public VnmDbContext(DbContextOptions<VnmDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<ConsumptionReading> ConsumptionReadings { get; set; }

    public virtual DbSet<DailyEnergyBalance> DailyEnergyBalances { get; set; }

    public virtual DbSet<InverterInfo> InverterInfos { get; set; }

    public virtual DbSet<InverterReading> InverterReadings { get; set; }

    public virtual DbSet<ProviderSettlement> ProviderSettlements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.Country).HasMaxLength(50);
            entity.Property(e => e.County).HasMaxLength(50);
            entity.Property(e => e.PostalCode).HasMaxLength(50);
            entity.Property(e => e.Street).HasMaxLength(50);
            entity.Property(e => e.StreetNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(225);
            entity.Property(e => e.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.Property(e => e.RoleId).HasMaxLength(225);

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(225);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.ExternalSubjectId).HasMaxLength(225);
            entity.Property(e => e.PhoneNumber).HasMaxLength(127);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.IndexerProperty<string>("UserId").HasMaxLength(225);
                        j.IndexerProperty<string>("RoleId").HasMaxLength(225);
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.Property(e => e.UserId).HasMaxLength(225);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<ConsumptionReading>(entity =>
        {
            entity.Property(e => e.Power).HasMaxLength(50);
            entity.Property(e => e.Source).HasMaxLength(50);

            entity.HasOne(d => d.Location).WithMany(p => p.ConsumptionReadings)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_ConsumptionReadings_Addresses");
        });

        modelBuilder.Entity<DailyEnergyBalance>(entity =>
        {
            entity.Property(e => e.ConsumedKwh).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.DeficitKwh).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.ProducedKwh).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.SurplusKwh).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Location).WithMany(p => p.DailyEnergyBalances)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_DailyEnergyBalances_Addresses");
        });

        modelBuilder.Entity<InverterInfo>(entity =>
        {
            entity.Property(e => e.Manufacturer).HasMaxLength(50);
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.SerialNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<InverterReading>(entity =>
        {
            entity.Property(e => e.Source).HasMaxLength(50);

            entity.HasOne(d => d.Inverter).WithMany(p => p.InverterReadings)
                .HasForeignKey(d => d.InverterId)
                .HasConstraintName("FK_InverterReadings_InverterInfos");

            entity.HasOne(d => d.Location).WithMany(p => p.InverterReadings)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_InverterReadings_Addresses");
        });

        modelBuilder.Entity<ProviderSettlement>(entity =>
        {
            entity.Property(e => e.AcceptedKwh).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.EnergyCreditKwh).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.InjectedKwh).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.Mode).HasMaxLength(50);
            entity.Property(e => e.MonetaryCredit).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.RatePerKwh).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Location).WithMany(p => p.ProviderSettlements)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_ProviderSettlements_Addresses");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
