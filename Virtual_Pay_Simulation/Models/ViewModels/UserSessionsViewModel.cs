using System;
using System.Collections.Generic;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	// Bu, seans detaylarını tutacak olan iç model
	public class UserSessionDetailsViewModel
	{
		public int ApplicationRequestId { get; set; }
		public string ServiceTitle { get; set; }
		public TimeSpan StartTime { get; set; }
		public TimeSpan EndTime { get; set; }
		public DateTime ServiceStartDate { get; set; }
		public DateTime ServiceEndDate { get; set; }
		public string Days { get; set; }
		public int SessionId { get; set; }
		public bool IsRequest { get; set; }
	}

	// Bu, seanslar sayfasında tüm listeleri taşıyacak olan ana model
	public class UserSessionsViewModel
	{
		public List<SessionGroupViewModel> AppliedSessions { get; set; }
		public List<SessionGroupViewModel> PurchasedSessions { get; set; }
		public List<SessionGroupViewModel> PastSessions { get; set; }
	}
}
