using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Virtual_Pay_Simulation.Models
{
	public class ApplicationRequest
	{
		[Key]
		public int ApplicationRequestId { get; set; }

		[Required]
		public string UserId { get; set; }

		[ForeignKey("UserId")]
		public ApplicationUser ApplicationUser { get; set; }

		[Required]
		public int SessionId { get; set; }

		[ForeignKey("SessionId")]
		public Session Session { get; set; }

		[Required]
		public DateTime RequestDate { get; set; } = DateTime.Now;

		[Required]
		public bool PaymentComplete { get; set; } = false;

		[Required]
		public bool IsRequest { get; set; }

		public EnrollmentStatus EnrollmentStatus { get; set; }
	}

}
