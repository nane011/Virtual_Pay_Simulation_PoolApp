using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Virtual_Pay_Simulation.Models
{
	public class Service
	{
		[Key]
		public int ServiceId { get; set; }

		[Required]
		public string Title { get; set; }

		[Required]
		public string Description { get; set; }

		[Required]
		public decimal StudentPrice { get; set; }
		[Required]
		public decimal StaffPrice { get; set; }

		[Required]
		public decimal MemberPrice { get; set; }

		[Required]
		public DateTime StartTime { get; set; }

		[Required]
		public DateTime EndTime { get; set; }

		[Required]
		public bool IsActive { get; set; } = false;

		[Required]
		public DateTime PayDueDate { get; set; }
		//Navigation
		public ICollection<Session> Sessions { get; set; }
	}
}
