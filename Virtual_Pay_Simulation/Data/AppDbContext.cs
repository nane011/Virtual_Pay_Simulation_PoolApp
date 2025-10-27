using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Virtual_Pay_Simulation.Models;

namespace Virtual_Pay_Simulation.Data

{
	public class AppDbContext:IdentityDbContext<ApplicationUser>
	{
		public AppDbContext(DbContextOptions<AppDbContext>options): base(options) { }
		public DbSet<ApplicationUser> ApplicationUsers { get; set; }
		public DbSet<Announcement> Announcements { get; set; }
		public DbSet<Purchase>Purchases { get; set; }
		public DbSet<Service> Services { get; set; }
		public DbSet<Session> Sessions { get; set; }
		public DbSet <Attendance> Attendaces { get; set; }
		public DbSet<ApplicationRequest>ApplicationRequests { get; set; }
		public DbSet<UserPenalty> UserPenalties { get; set; }
		public DbSet<ServiceDay> ServiceDays { get; set; }
		public DbSet<ServiceDayTemplate> ServiceDayTemplates { get; set; }
		public DbSet<Contract> Contracts { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Purchase>()
				.Property(p => p.PaidAmount)
				.HasColumnType("decimal(18,2)");

			modelBuilder.Entity<Service>()
				.Property(c => c.StudentPrice)
				.HasColumnType("decimal(18,2)");

			modelBuilder.Entity<Service>()
				.Property(c => c.StaffPrice)
				.HasColumnType("decimal(18,2)");

			modelBuilder.Entity<Service>()
				.Property(c => c.MemberPrice)
				.HasColumnType("decimal(18,2)");
		}
	}
}
