using System;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class SessionGroupViewModel
	{
		// Satın alınmış grupları yoklama sayfasına bağlamak için kullanılır.
		public int PurchaseId { get; set; }
		public string ServiceTitle { get; set; }
		public TimeSpan StartTime { get; set; }
		public TimeSpan EndTime { get; set; }
		public DateTime ServiceStartDate { get; set; }
		public DateTime ServiceEndDate { get; set; }
		public string Days { get; set; }
	}
}
