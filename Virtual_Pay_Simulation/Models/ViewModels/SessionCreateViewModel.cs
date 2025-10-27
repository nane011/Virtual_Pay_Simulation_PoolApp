using System.ComponentModel.DataAnnotations;
using Virtual_Pay_Simulation.Models;

public class SessionCreateViewModel
{
	[Required]
	public int ServiceId { get; set; }

	[Required(ErrorMessage = "Başlangıç saati zorunludur")]
	[Display(Name = "Başlangıç Saati")]
	public TimeSpan StartTime { get; set; }

	[Required(ErrorMessage = "Bitiş saati zorunludur")]
	[Display(Name = "Bitiş Saati")]
	public TimeSpan EndTime { get; set; }

	[Required(ErrorMessage = "Minimum kapasite zorunludur")]
	[Display(Name = "Minimum Kapasite")]
	[Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kapasite girin")]
	public int MinCapacity { get; set; }

	[Required(ErrorMessage = "Sınır kapasite zorunludur")]
	[Display(Name = "Sınır Kapasite")]
	[Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kapasite girin")]
	public int BorderCapacity { get; set; }

	[Required(ErrorMessage = "Maksimum kapasite zorunludur")]
	[Display(Name = "Maksimum Kapasite")]
	[Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kapasite girin")]
	public int MaxCapacity { get; set; }

	[Required(ErrorMessage = "Tarih zorunludur")]
	[Display(Name = "Seans Tarihi")]
	public DateTime Date { get; set; }

	[Display(Name = "Aktif Mi?")]
	public bool IsActive { get; set; } = false;

	public List<ServiceDay> ServiceDays { get; set; }
	public List<int> SelectedServiceDayIds { get; set; } = new List<int>();
}