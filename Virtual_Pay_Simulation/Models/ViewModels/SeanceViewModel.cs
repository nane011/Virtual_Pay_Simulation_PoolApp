using System.ComponentModel.DataAnnotations;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class SeanceViewModel
	{
		public int ServiceId { get; set; }
		public string? ServiceTitle { get; set; }

		public List<int> SelectedTemplateIds { get; set; } = new();
		public List<ServiceDayTemplate> Templates { get; set; } = new();

		public List<SeanceInput> Seances { get; set; } = new();
	}
}
