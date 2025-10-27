using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Virtual_Pay_Simulation.Models
{
	public class UserPenalty
	{
		[Key]
		public int PenaltyId { get; set; }

		[Required]
		public string UserId { get; set; }

		[ForeignKey("UserId")]
		public ApplicationUser ApplicationUser { get; set; }

		[Required]
		public int SessionId { get; set; }

		[ForeignKey("SessionId")]
		public Session Session { get; set; }

		[Required]
		public string Reason { get; set; } //alınan ceza sebebi.
	}
}
