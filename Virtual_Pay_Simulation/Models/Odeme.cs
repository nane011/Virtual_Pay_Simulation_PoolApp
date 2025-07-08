using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Virtual_Pay_Simulation.Models
{
	public class Odeme
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int Price { get; set; }

		[Required]
		public bool Situation { get; set; } = false;

		[Required]
		public int Person {  get; set; }

		[Required]
		public int Session { get; set; }

		[ForeignKey("Person")]
		public Uye Uye { get; set; }

		[ForeignKey("Session")]
		public Session Sess { get; set; }
	}
}
