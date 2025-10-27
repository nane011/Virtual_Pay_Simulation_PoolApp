namespace Virtual_Pay_Simulation.Models.ViewModels
{
	public class SeanceInput
	{
		public TimeSpan StartTime { get; set; }
		public TimeSpan EndTime { get; set; }
		public int MinCapacity { get; set; }
		public int BorderCapacity { get; set; }
		public int MaxCapacity { get; set; }
	}
}
