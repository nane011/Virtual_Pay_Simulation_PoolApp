using System.Collections.Generic;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class SessionAttendanceViewModel
	{
		public string ServiceTitle { get; set; }
		public string SessionTime { get; set; }
		public string SessionDays { get; set; }
		public List<Attendance> AttendanceRecords { get; set; }
	}
}
