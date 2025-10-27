namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class SeanceGroupModel
	{
		public string TemplateName { get; set; }
		public Gender Gender { get; set; }
		public TimeSpan StartTime { get; set; }
		public TimeSpan EndTime { get; set; }
		public int MinCapacity { get; set; }
		public int BorderCapacity { get; set; }
		public int MaxCapacity { get; set; }
		public int CurrentCapacity { get; set; } // Ödeme yapanlar
		public int ApplicantCount { get; set; }  // YENİ: Sadece başvuru yapanlar
		public List<string> Days { get; set; }
		public List<int> SessionIds { get; set; }
		public List<int> CurrentCapacities { get; set; }
	}

}
