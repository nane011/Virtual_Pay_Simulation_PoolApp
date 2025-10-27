using System.Collections.Generic;
using Virtual_Pay_Simulation.Models;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class AdminParticipantsViewModel
	{
		public List<ApplicationUser> Applicants { get; set; }
		public List<ApplicationUser> Participants { get; set; }
	}
}