using Microsoft.EntityFrameworkCore;
using SaucyMVC.Models;
namespace SaucyMVC.Data;
public class SaucyDbContext:DbContext {
 public SaucyDbContext(DbContextOptions<SaucyDbContext> o):base(o){}
 public DbSet<User> Users=>Set<User>(); public DbSet<DiningTable> DiningTables=>Set<DiningTable>(); public DbSet<MenuItem> MenuItems=>Set<MenuItem>(); public DbSet<SauceOption> SauceOptions=>Set<SauceOption>(); public DbSet<Reservation> Reservations=>Set<Reservation>(); public DbSet<Order> Orders=>Set<Order>(); public DbSet<OrderItem> OrderItems=>Set<OrderItem>(); public DbSet<OrderStatusHistory> OrderStatusHistory=>Set<OrderStatusHistory>(); public DbSet<Payment> Payments=>Set<Payment>(); public DbSet<RefundRequest> RefundRequests=>Set<RefundRequest>(); public DbSet<TransactionHistory> TransactionHistory=>Set<TransactionHistory>();
 protected override void OnModelCreating(ModelBuilder b){
  b.Entity<User>().HasIndex(x=>x.Email).IsUnique(); b.Entity<DiningTable>().HasIndex(x=>x.TableNumber).IsUnique(); b.Entity<SauceOption>().HasIndex(x=>x.ImagePath).IsUnique();
  b.Entity<MenuItem>().Property(x=>x.Price).HasPrecision(10,2); b.Entity<Order>().Property(x=>x.TotalAmount).HasPrecision(10,2); b.Entity<OrderItem>().Property(x=>x.UnitPrice).HasPrecision(10,2); b.Entity<Payment>().Property(x=>x.Amount).HasPrecision(10,2); b.Entity<RefundRequest>().Property(x=>x.Amount).HasPrecision(10,2); b.Entity<TransactionHistory>().Property(x=>x.TotalAmount).HasPrecision(10,2);
  b.Entity<OrderStatusHistory>().HasOne(x=>x.Order).WithMany(x=>x.StatusHistory).HasForeignKey(x=>x.OrderId).OnDelete(DeleteBehavior.Cascade);
 }
}
