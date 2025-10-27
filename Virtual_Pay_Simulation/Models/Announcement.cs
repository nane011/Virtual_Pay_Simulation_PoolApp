using System.ComponentModel.DataAnnotations;


namespace Virtual_Pay_Simulation.Models
{
	public class Announcement
	{
		[Key]
		public int AnnouncementId { get; set; }

		[Required]
		public string Title { get; set; }

		[Required]
		public string Description { get; set; }

		[Required]
		public string? Description2 { get; set; }

		[Required]
		public DateTime AddDate { get; set; } = DateTime.Now;

		[Required]
		public DateTime EndDate { get; set; }
	}
}
