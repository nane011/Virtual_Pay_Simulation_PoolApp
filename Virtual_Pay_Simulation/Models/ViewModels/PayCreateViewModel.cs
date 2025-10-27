using Virtual_Pay_Simulation.Models;
using System.Collections.Generic;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class PayCreateViewModel
	{
		public ApplicationUser User { get; set; }
		public decimal PaidAmount { get; set; }
		public string SessionIds { get; set; }
		public string ServiceTitle { get; set; }
		public int ServiceId { get; set; }
	}
}
