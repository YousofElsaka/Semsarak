using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SEMSARK.Models;

namespace SEMSARK.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser,Role,string>
    {
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Payment>()
                .HasOne(p => p.Owner)
                .WithMany(u => u.PaymentsAsOwner)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Renter)
                .WithMany(u => u.PaymentsAsRenter)
                .HasForeignKey(p => p.RenterId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}