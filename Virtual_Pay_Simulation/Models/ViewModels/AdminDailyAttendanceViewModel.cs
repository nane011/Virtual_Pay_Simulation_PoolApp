using System;
using System.Collections.Generic;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class AdminDailyAttendanceViewModel
	{
		public DateTime Date { get; set; }
		public bool IsPastDate { get; set; }
		public bool IsFutureDate { get; set; }
		public List<UserAttendanceStatus> Attendees { get; set; }
	}

	public class UserAttendanceStatus
	{
		public string FullName { get; set; }
		public string TCKimlikNo { get; set; }
		public bool IsPresent { get; set; }
	}
}
