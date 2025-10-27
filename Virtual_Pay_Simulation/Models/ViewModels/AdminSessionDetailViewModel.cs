using System;
using System.Collections.Generic;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class AdminSessionDetailViewModel
	{
		public int ServiceId { get; set; }
		public string ServiceTitle { get; set; }
		public string GroupIdentifier { get; set; }
		public string SessionInfo { get; set; }
		public List<DateTime> SessionDates { get; set; }
	}
}
