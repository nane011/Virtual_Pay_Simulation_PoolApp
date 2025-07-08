using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace Virtual_Pay_Simulation.Models
{
	public class SessionPerson
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int Person { get; set; }

		[Required]
		public int Session {  get; set; }

		[Required]
		public DateTime PurchaseDate { get; set; }

		[Required]
		public DateTime ExpDate { get; set; }


		[ForeignKey("Person")]
		public Uye Uye { get; set; }

		[ForeignKey("Session")]
		public Session Sess { get; set; }
	}
}
