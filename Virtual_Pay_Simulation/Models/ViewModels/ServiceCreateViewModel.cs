using System.ComponentModel.DataAnnotations;
using Virtual_Pay_Simulation.Models;

public class ServiceCreateViewModel
{
	public int ServiceId { get; set; }

	[Required(ErrorMessage = "Başlık zorunludur")]
	public string Title { get; set; }

	[Required(ErrorMessage = "Açıklama zorunludur")]
	public string Description { get; set; }

	[Required(ErrorMessage = "Öğrenci ücreti zorunludur")]
	[Range(0, double.MaxValue, ErrorMessage = "Geçerli bir ücret girin")]
	public decimal StudentPrice { get; set; }

	[Required(ErrorMessage = "Personel ücreti zorunludur")]
	[Range(0, double.MaxValue, ErrorMessage = "Geçerli bir ücret girin")]
	public decimal StaffPrice { get; set; }

	[Required(ErrorMessage = "Üye ücreti zorunludur")]
	[Range(0, double.MaxValue, ErrorMessage = "Geçerli bir ücret girin")]
	public decimal MemberPrice { get; set; }

	[Required(ErrorMessage = "Başlangıç tarihi zorunludur")]
	[Display(Name = "Başlangıç Tarihi")]
	public DateTime StartTime { get; set; }

	[Required(ErrorMessage = "Bitiş tarihi zorunludur")]
	[Display(Name = "Bitiş Tarihi")]
	public DateTime EndTime { get; set; }

	[Required]
	public bool IsActive { get; set; } = false;

	[Required(ErrorMessage = "Son Ödeme tarihi zorunludur")]
	[Display(Name = "Başvuru Yapanlar İçin Son Ödeme Tarihi")]
	public DateTime PayDueDate { get; set; }
}