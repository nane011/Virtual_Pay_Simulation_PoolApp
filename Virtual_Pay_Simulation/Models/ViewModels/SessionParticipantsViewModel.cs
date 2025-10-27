using System;
using System.Collections.Generic;
using Virtual_Pay_Simulation.Models;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class ParticipantViewModel
	{
		public string UserId { get; set; }
		public string Name { get; set; }
		public string Surname { get; set; }
		public string TCKimlikNo { get; set; }
		public string Status { get; set; }
	}
	public class SessionParticipantsViewModel
	{
		public int ServiceId { get; set; }
		public int SessionId { get; set; }
		public string ServiceTitle { get; set; }
		public TimeSpan StartTime { get; set; }
		public TimeSpan EndTime { get; set; }
		public List<ParticipantViewModel> Participants { get; set; }
	}
}
