using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Virtual_Pay_Simulation.Models
{
	public class ApplicationUser : IdentityUser
	{
		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[Required]
		[StringLength(100)]
		public string Surname { get; set; }

		[Required]
		public bool WBandState { get; set; } = false;

		public string? WristBand { get; set; }

		[Required]
		public Gender Gender { get; set; }

		[Required]
		public bool IsBlocked { get; set; } = false;

		[Required]
		public UserType UserType { get; set; }

		public int? AcceptedContractVersion { get; set; }

		public DateTime? ContractAccepptanceDate { get; set; }

		public ICollection<UserPenalty> UserPenalties { get; set; }
	}
}