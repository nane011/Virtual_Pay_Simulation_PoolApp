using System;
using System.ComponentModel.DataAnnotations;

namespace Virtual_Pay_Simulation.Models
{
	public class Contract
	{
		[Key]
		public int ContractId { get; set; }

		[Required(ErrorMessage = "Sözleşme başlığı zorunludur.")]
		[Display(Name = "Sözleşme Başlığı")]
		public string Title { get; set; }

		[Required(ErrorMessage = "Sözleşme içeriği boş olamaz.")]
		[Display(Name = "Sözleşme İçeriği")]
		public string Content { get; set; }

		[Display(Name = "Versiyon Numarası")]
		public int Version { get; set; }

		[Display(Name = "Aktif Versiyon mu?")]
		public bool IsActive { get; set; }

		[Display(Name = "Oluşturulma Tarihi")]
		public DateTime CreatedDate { get; set; }
	}
}