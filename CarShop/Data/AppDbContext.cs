using System;
using System.Collections.Generic;
using CarShop.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarShop.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Car> Cars { get; set; }

    public virtual DbSet<CarImage> CarImages { get; set; }

    public virtual DbSet<CarPost> CarPosts { get; set; }

    public virtual DbSet<CarPostComment> CarPostComments { get; set; }

    public virtual DbSet<Offer> Offers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalesRecord> SalesRecords { get; set; }

    public virtual DbSet<TradeInImage> TradeInImages { get; set; }

    public virtual DbSet<TradeInRequest> TradeInRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserFavorite> UserFavorites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Branches_CreatedAt");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasIndex(e => e.BranchId, "IX_Cars_BranchId");

            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Cars_CreatedAt");
            entity.Property(e => e.FuelType).HasMaxLength(20);
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Transmission).HasMaxLength(20);

            entity.HasOne(d => d.Branch).WithMany(p => p.Cars)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cars_Branches");
        });

        modelBuilder.Entity<CarImage>(entity =>
        {
            entity.HasIndex(e => e.CarId, "IX_CarImages_CarId");

            entity.HasIndex(e => new { e.CarId, e.IsPrimary }, "UX_CarImages_CarId_Primary")
                .IsUnique()
                .HasFilter("([IsPrimary]=(1))");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_CarImages_CreatedAt");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);

            entity.HasOne(d => d.Car).WithMany(p => p.CarImages)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarImages_Cars");
        });

        modelBuilder.Entity<CarPost>(entity =>
        {
            entity.HasIndex(e => e.CarId, "IX_CarPosts_CarId");

            entity.HasIndex(e => e.UserId, "IX_CarPosts_UserId");

            entity.Property(e => e.Category).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_CarPosts_CreatedAt");
            entity.Property(e => e.Title).HasMaxLength(50);

            entity.HasOne(d => d.Car).WithMany(p => p.CarPosts)
                .HasForeignKey(d => d.CarId)
                .HasConstraintName("FK_CarPosts_Cars");

            entity.HasOne(d => d.User).WithMany(p => p.CarPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarPosts_Users");
        });

        modelBuilder.Entity<CarPostComment>(entity =>
        {
            entity.HasIndex(e => e.ParentId, "IX_CarPostComments_ParentId");

            entity.HasIndex(e => e.PostId, "IX_CarPostComments_PostId");

            entity.HasIndex(e => e.UserId, "IX_CarPostComments_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_CarPostComments_CreatedAt");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_CarPostComments_Parent");

            entity.HasOne(d => d.Post).WithMany(p => p.CarPostComments)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarPostComments_CarPosts");

            entity.HasOne(d => d.User).WithMany(p => p.CarPostComments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CarPostComments_Users");
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasIndex(e => e.CarId, "IX_Offers_CarId");

            entity.HasIndex(e => e.UserId, "IX_Offers_UserId");

            entity.HasIndex(e => new { e.UserId, e.CarId }, "UX_Offers_UserId_CarId_Pending")
                .IsUnique()
                .HasFilter("([Status]=N'Pending')");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Offers_CreatedAt");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Car).WithMany(p => p.Offers)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Offers_Cars");

            entity.HasOne(d => d.User).WithMany(p => p.Offers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Offers_Users");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.CarId, "IX_Orders_CarId");

            entity.HasIndex(e => e.UserId, "IX_Orders_UserId");

            entity.HasIndex(e => new { e.CarId, e.PaymentStatus }, "UX_Orders_CarId_Pending")
                .IsUnique()
                .HasFilter("([PaymentStatus]=N'Pending')");

            entity.HasIndex(e => e.OrderNo, "UX_Orders_OrderNo").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Orders_CreatedAt");
            entity.Property(e => e.OrderNo).HasMaxLength(20);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(20);
            entity.Property(e => e.TradeNo).HasMaxLength(100);

            entity.HasOne(d => d.Car).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Cars");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasIndex(e => e.CarId, "IX_Reservations_CarId");

            entity.HasIndex(e => e.UserId, "IX_Reservations_UserId");

            entity.HasIndex(e => new { e.UserId, e.CarId }, "UX_Reservations_UserId_CarId_Pending")
                .IsUnique()
                .HasFilter("([Status]=N'Pending')");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Reservations_CreatedAt");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Car).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservations_Cars");

            entity.HasOne(d => d.User).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservations_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name, "UX_Roles_Name").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<SalesRecord>(entity =>
        {
            entity.HasIndex(e => e.CarId, "IX_SalesRecords_CarId");

            entity.HasIndex(e => e.UserId, "IX_SalesRecords_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_SalesRecords_CreatedAt");

            entity.HasOne(d => d.Car).WithMany(p => p.SalesRecords)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesRecords_Cars");

            entity.HasOne(d => d.User).WithMany(p => p.SalesRecords)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesRecords_Users");
        });

        modelBuilder.Entity<TradeInImage>(entity =>
        {
            entity.HasIndex(e => e.TradeInRequestId, "IX_TradeInImages_TradeInRequestId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_TradeInImages_CreatedAt");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);

            entity.HasOne(d => d.TradeInRequest).WithMany(p => p.TradeInImages)
                .HasForeignKey(d => d.TradeInRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TradeInImages_TradeInRequests");
        });

        modelBuilder.Entity<TradeInRequest>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_TradeInRequests_UserId");

            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_TradeInRequests_CreatedAt");
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.User).WithMany(p => p.TradeInRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TradeInRequests_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => new { e.AuthProvider, e.ProviderId }, "UX_Users_AuthProvider_ProviderId")
                .IsUnique()
                .HasFilter("([ProviderId] IS NOT NULL)");

            entity.HasIndex(e => e.Email, "UX_Users_Email").IsUnique();

            entity.HasIndex(e => e.IdentityNumberHash, "UX_Users_IdentityNumberHash")
                .IsUnique()
                .HasFilter("([IdentityNumberHash] IS NOT NULL)");

            entity.Property(e => e.Address).HasMaxLength(100);
            entity.Property(e => e.AuthProvider).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Users_CreatedAt");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.IdentityNumber).HasMaxLength(500);
            entity.Property(e => e.IdentityNumberHash).HasMaxLength(64);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Password).HasMaxLength(60);
            entity.Property(e => e.Phone).HasMaxLength(10);
            entity.Property(e => e.ProviderId).HasMaxLength(255);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<UserFavorite>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.CarId });

            entity.HasIndex(e => e.CarId, "IX_UserFavorites_CarId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_UserFavorites_CreatedAt");

            entity.HasOne(d => d.Car).WithMany(p => p.UserFavorites)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserFavorites_Cars");

            entity.HasOne(d => d.User).WithMany(p => p.UserFavorites)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserFavorites_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
