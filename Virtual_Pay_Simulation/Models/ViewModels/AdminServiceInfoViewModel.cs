using System.Collections.Generic;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class AdminServiceInfoViewModel
	{
		public Service Service { get; set; }
		public List<AdminSessionGroupViewModel> SessionGroups { get; set; }
	}

	public class AdminSessionGroupViewModel
	{
		public string GroupIdentifier { get; set; } // Seans grubunu benzersiz olarak tanımlamak için
		public string TemplateName { get; set; }
		public Gender Gender { get; set; }
		public TimeSpan StartTime { get; set; }
		public TimeSpan EndTime { get; set; }
		public string Days { get; set; }
		public int ApplicantCount { get; set; }
		public int ParticipantCount { get; set; }
		public int MaxCapacity { get; set; }
	}

	// --- YENİ EKLENECEK VIEWMODELLAR ---

	public class UserAttendanceHistoryViewModel
	{
		public string UserId { get; set; }
		public string UserFullName { get; set; }
		public List<AttendanceRecordViewModel> HistoryRecords { get; set; }

		public DateTime? FilterDate { get; set; }
		public bool FilterShowOnlyAttended { get; set; }
	}

	public class AttendanceRecordViewModel
	{
		public DateTime AttendanceDate { get; set; }
		public string ServiceName { get; set; }
		public string SessionDay { get; set; }
		public string SessionTime { get; set; }
		public bool IsPresent { get; set; }
	}
}
