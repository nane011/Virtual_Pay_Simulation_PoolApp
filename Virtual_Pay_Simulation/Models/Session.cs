using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Virtual_Pay_Simulation.Models
{
	public class Session
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string Explanation { get; set; }

		[Required]
		public string Explan2 { get; set; }

		[Required]
		public int Price { get; set; }

	}
}
