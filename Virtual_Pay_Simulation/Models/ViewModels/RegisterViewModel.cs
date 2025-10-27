using System.ComponentModel.DataAnnotations;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class RegisterViewModel
	{
		[Required]
		public string Name { get; set; }

		[Required]
		public string Surname { get; set; }

		[Required]
		[StringLength(11, MinimumLength = 11)]
		public string TC { get; set; }

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[Phone]
		public string Phone { get; set; }

		public string? WristBand { get; set; }

		[Required]
		public Gender Gender { get; set; }
		public int RoleId { get; set; } // 1: Admin, 2: User
		public UserType UserType { get; set; } // enum ise bu
	}
}