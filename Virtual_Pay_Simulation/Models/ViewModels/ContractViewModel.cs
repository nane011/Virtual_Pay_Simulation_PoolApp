namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class ContractViewModel
	{
		public string Title { get; set; }
		public string Content { get; set; }
		public int Version { get; set; }
		// Kullanıcının satın almak istediği seans grubunun ID'lerini
		// onaydan sonra ödeme sayfasına taşıyabilmek için bu alana ihtiyacımız var.
		public string SessionIds { get; set; }
	}
}