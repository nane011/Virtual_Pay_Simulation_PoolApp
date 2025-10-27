
namespace Virtual_Pay_Simulation.Interfaces
{
	public interface IEmailService
	{
		Task SendPasswordEmail(string recipientEmail, string generatedPassword);
		Task SendEmailAsync(string toEmail, string subject, string body);
	}
}
