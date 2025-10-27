using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Virtual_Pay_Simulation.Models
{
	public class Attendance
	{
		[Key]
		public int AttendanceId { get; set; }

		[Required]
		public string UserId { get; set; }

		[ForeignKey("UserId")]
		public ApplicationUser ApplicationUser { get; set; }

		[Required]
		public int SessionId { get; set; }

		[ForeignKey("SessionId")]
		public Session Session { get; set; }

		[Required]
		public bool IsPresent { get; set; } = false; // Kullanıcının seansa gelip gelmediği bilgisi

		[Required]
		public DateTime AttendanceDate { get; set; } // Katılımın kaydedildiği tarih ve saat

		[Required]
		public TimeSpan StartTime { get; set; }

		[Required]
		public TimeSpan EndTime { get; set; }
	}
}
