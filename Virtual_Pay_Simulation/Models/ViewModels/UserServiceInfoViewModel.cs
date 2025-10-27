using Virtual_Pay_Simulation.Models;
using System.Collections.Generic;
using System;

namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class UserServiceInfoViewModel
	{
		public string Title { get; set; }
		public DateTime PayDueDate { get; set; }
		public List<SeanceGroupModel> SeanceGroups { get; set; }
		public List<int> UserAppliedSessionIds { get; set; }

		// Hata düzeltmesi: Bu listeyi ekliyoruz.
		public List<int> UserPaidSessionIds { get; set; }

		public Service Service { get; set; } // Tüm hizmet bilgilerini tutmak için
		public UserType UserType { get; set; } // Kullanıcının tipini tutmak için
		public bool UserHasPurchasedInService { get; set; }
	}
}
