// Mevcut Session modeliniz
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace Virtual_Pay_Simulation.Models
{
	public class Session
	{
		[Key]
		public int SessionId { get; set; }

		[Required]
		public TimeSpan StartTime { get; set; }

		[Required]
		public TimeSpan EndTime { get; set; }

		[Required]
		public int MinCapacity { get; set; }

		[Required]
		public int BorderCapacity { get; set; }

		[Required]
		public int MaxCapacity { get; set; }

		[Required]
		public int? CurrentCapacity { get; set; }

		[Required]
		public bool IsActive { get; set; } = false;

		[Required]
		public int ServiceDayId { get; set; }

		[ForeignKey("ServiceDayId")]
		public ServiceDay ServiceDay { get; set; }

		[Required]
		public int ServiceId { get; set; }

		[ForeignKey("ServiceId")]
		public Service Service { get; set; }

		// Navigation
		public ICollection<Purchase> Purchases { get; set; }

		// Navigation
		public ICollection<ApplicationRequest> ApplicationRequests { get; set; }

		// *** YENİ EKLENDİ ***
		// Bu seansa ait tüm katılım kayıtlarını tutmak için
		public ICollection<Attendance> Attendances { get; set; }
	}
}
