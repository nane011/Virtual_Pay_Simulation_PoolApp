using System.ComponentModel.DataAnnotations;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class LoginViewModel
	{
		[Required(ErrorMessage = "TC kimlik numarası zorunludur.")]
		[StringLength(11, MinimumLength = 11, ErrorMessage = "TC 11 karakter olmalıdır.")]
		public string TC { get; set; }

		[Required(ErrorMessage = "Şifre zorunludur.")]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		[Display(Name = "Beni hatırla")]
		public bool RememberMe { get; set; }

	}
}