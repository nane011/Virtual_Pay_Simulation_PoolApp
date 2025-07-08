using System.ComponentModel.DataAnnotations;


namespace Virtual_Pay_Simulation.Models
{
	public class Uye
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string Name { get; set; }

		[Required]
		public string Surname { get; set; }

		[Required]
		public string Password { get; set; }

		[Required]
		public long TC {  get; set; }

		[Required]
		public int WristBand { get; set; }

		[Required]
		public bool WBandState { get; set; } = false;

		//Navigation yani foreign key tanım
		public ICollection<Odeme> Odemeler { get; set; }
	}
}
