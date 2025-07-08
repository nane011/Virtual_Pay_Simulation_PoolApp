using Microsoft.EntityFrameworkCore;
using Virtual_Pay_Simulation.Models;

namespace Virtual_Pay_Simulation.Data

{
	public class AppDbContext:DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options): base(options) { }
		public DbSet<Uye> Uyeler { get; set; }
		public DbSet<Odeme>Odemeler { get; set; }
		public DbSet<Session> Sessions { get; set; }
		public DbSet<SessionPerson> SessionsPerson { get; set; }
	}
}
