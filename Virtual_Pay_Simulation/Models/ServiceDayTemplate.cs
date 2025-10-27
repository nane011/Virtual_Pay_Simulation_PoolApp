using System.ComponentModel.DataAnnotations;

namespace Virtual_Pay_Simulation.Models
{
	public class ServiceDayTemplate
	{
		[Key]
		public int TemplateId { get; set; }

		[Required]
		public string TemplateName { get; set; }

		[Required]
		public Gender Gender { get; set; }

		public ICollection<ServiceDay> Days { get; set; }
	}
}
