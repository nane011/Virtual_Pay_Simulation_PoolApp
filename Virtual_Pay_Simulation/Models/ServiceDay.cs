using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Virtual_Pay_Simulation.Models
{
	public class ServiceDay
	{
		[Key]
		public int ServiceDayId { get; set; }

		[Required]
		public DayOfWeek Day { get; set; } // Pazartesi, Salı vs.

		[Required]
		public int TemplateId { get; set; }

		[ForeignKey("TemplateId")]
		public ServiceDayTemplate ServiceDayTemplate { get; set; }

		//navigation
		public ICollection<Session> Sessions { get; set; }
	}
}
	