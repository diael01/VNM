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

    public virtual DbSet<TransferWorkflow> TransferWorkflows { get; set; }

    public virtual DbSet<TransferRequest> TransferRequests { get; set; }

    public virtual DbSet<TransferRule> TransferRules { get; set; }

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
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

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
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                        j.IndexerProperty<string>("UserId").HasMaxLength(225);
                        j.IndexerProperty<string>("RoleId").HasMaxLength(225);
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.Property(e => e.UserId).HasMaxLength(225);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<ConsumptionReading>(entity =>
        {
            entity.HasIndex(e => e.AddressId, "IX_ConsumptionReadings_AddressId");

            entity.Property(e => e.Power).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.Source).HasMaxLength(50);

            entity.HasOne(d => d.Address).WithMany(p => p.ConsumptionReadings).HasForeignKey(d => d.AddressId);
        });

        modelBuilder.Entity<DailyEnergyBalance>(entity =>
        {
            entity.HasIndex(e => e.AddressId, "IX_DailyEnergyBalances_AddressId");

            entity.HasIndex(e => e.InverterInfoId, "IX_DailyEnergyBalances_InverterInfoId");

            entity.Property(e => e.ConsumedKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.DeficitKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.InverterInfoId).HasDefaultValue(0, "DF__DailyEner__Inver__628FA481");
            entity.Property(e => e.NetKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.NetPerAddressKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.ProducedKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.SurplusKwh).HasColumnType("decimal(18, 5)");

            entity.HasOne(d => d.Address).WithMany(p => p.DailyEnergyBalances)
                .HasForeignKey(d => d.AddressId)
                .HasConstraintName("FK_DailyEnergyBalances_Addresses");
        });

        modelBuilder.Entity<InverterInfo>(entity =>
        {
            entity.HasIndex(e => e.AddressId, "IX_InverterInfos_AddressId");

            entity.Property(e => e.Manufacturer).HasMaxLength(50);
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.SerialNumber).HasMaxLength(50);

            entity.HasOne(d => d.Address).WithMany(p => p.InverterInfos).HasForeignKey(d => d.AddressId);
        });

        modelBuilder.Entity<InverterReading>(entity =>
        {
            entity.HasIndex(e => e.InverterInfoId, "IX_InverterReadings_InverterInfoId");

            entity.Property(e => e.Current).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.Power).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.Voltage).HasColumnType("decimal(18, 5)");

            entity.HasOne(d => d.Address).WithMany(p => p.InverterReadings)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InverterReadings_Addresses");

            entity.HasOne(d => d.InverterInfo).WithMany(p => p.InverterReadings)
                .HasForeignKey(d => d.InverterInfoId)
                .HasConstraintName("FK_InverterReadings_InverterInfos");
        });

        modelBuilder.Entity<ProviderSettlement>(entity =>
        {
            entity.HasIndex(e => e.AddressId, "IX_ProviderSettlements_AddressId");

            entity.Property(e => e.AcceptedKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.EnergyCreditKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.InjectedKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.MonetaryCredit).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.RatePerKwh).HasColumnType("decimal(18, 5)");

            entity.HasOne(d => d.Address).WithMany(p => p.ProviderSettlements)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProviderSettlements_Addresses");
        });

        modelBuilder.Entity<TransferWorkflow>(entity =>
        {
            entity.ToTable("TransferWorkflow");

            entity.HasIndex(e => e.DestinationAddressId, "IX_TransferWorkflow_DestinationAddressId");

            entity.HasIndex(e => e.SourceAddressId, "IX_TransferWorkflow_SourceAddressId");

            entity.HasIndex(e => e.TransferRuleId, "IX_TransferWorkflow_TransferRuleId");

            entity.Property(e => e.AllocatedKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.RequestedKwh).HasColumnType("decimal(18, 5)");

            entity.HasOne(d => d.DestinationAddress).WithMany(p => p.TransferWorkflowDestinationAddresses)
                .HasForeignKey(d => d.DestinationAddressId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.SourceAddress).WithMany(p => p.TransferWorkflowSourceAddresses)
                .HasForeignKey(d => d.SourceAddressId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TransferRule).WithMany(p => p.TransferWorkflows)
                .HasForeignKey(d => d.TransferRuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TransferRequest>(entity =>
        {
            entity.Property(e => e.ActualAmount).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.RequestedAmount).HasColumnType("decimal(18, 5)");
        });

        modelBuilder.Entity<TransferRule>(entity =>
        {
            entity.HasIndex(e => e.DestinationAddressId, "IX_TransferRules_DestinationAddressId");

            entity.HasIndex(e => e.SourceAddressId, "IX_TransferRules_SourceAddressId");

            entity.Property(e => e.MaxDailyKwh).HasColumnType("decimal(18, 5)");
            entity.Property(e => e.WeightPercent).HasColumnType("decimal(18, 5)");

            entity.HasOne(d => d.DestinationAddress).WithMany(p => p.TransferRuleDestinationAddresses)
                .HasForeignKey(d => d.DestinationAddressId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.SourceAddress).WithMany(p => p.TransferRuleSourceAddresses)
                .HasForeignKey(d => d.SourceAddressId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
