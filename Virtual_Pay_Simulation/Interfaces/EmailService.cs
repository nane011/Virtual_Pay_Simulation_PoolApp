using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using System;

namespace Virtual_Pay_Simulation.Interfaces
{
	public class EmailService : IEmailService
	{
		private readonly string _smtpServer = "smtp.gmail.com";
		private readonly int _smtpPort = 587;
		private readonly string _smtpUsername = "gamzemisir04@gmail.com"; // Tam e-posta adresi
		private readonly string _smtpPassword = "vlse lhco mtxp xboq"; // Google Uygulama Şifresi

		public async Task SendPasswordEmail(string recipientEmail, string generatedPassword)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress("Sistem Yöneticisi", _smtpUsername));
			message.To.Add(new MailboxAddress("Kullanıcı", recipientEmail));
			message.Subject = "Hesap Şifreniz";

			message.Body = new TextPart("plain")
			{
				Text = $"Şifreniz:{generatedPassword}\n" +
		   "Giriş yapmak için bu şifreyi kullanabilirsiniz."
			};

			using (var client = new SmtpClient())
			{
				client.Connect(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
				client.Authenticate(_smtpUsername, _smtpPassword);
				client.Send(message);
				client.Disconnect(true);
			}
		}

		public async Task SendEmailAsync(string toEmail, string subject, string body)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress("Sistem Yöneticisi", _smtpUsername));
			message.To.Add(new MailboxAddress("Kullanıcı", toEmail));
			message.Subject = subject;

			// HTML içerik göndermek için TextFormat.Html kullanıyoruz
			message.Body = new TextPart(TextFormat.Html)
			{
				Text = body
			};

			using (var client = new SmtpClient())
			{
				await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
				await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
				await client.SendAsync(message);
				await client.DisconnectAsync(true);
			}
		}
	}
}