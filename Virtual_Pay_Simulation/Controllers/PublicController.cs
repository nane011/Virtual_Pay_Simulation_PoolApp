using Microsoft.AspNetCore.Mvc;
using Virtual_Pay_Simulation.Models;
using System.Linq;
using Virtual_Pay_Simulation.Data;
using Virtual_Pay_Simulation.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Virtual_Pay_Simulation.Models.ViewModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Virtual_Pay_Simulation.Controllers
{
	public class PublicController : Controller
	{
		private readonly AppDbContext _context; //bu context bağlantısı  
		private readonly IEmailService _emailService; // EmailService bağı  
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;

		public PublicController(AppDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) // Veritabanı bağlantısını alır
		{
			_context = context;
			_emailService = new EmailService(); // EmailService nesnesi
			_userManager = userManager;
			_signInManager = signInManager;
		}


		// Ana Hoşgeldiniz sayfası  
		[HttpGet]
		public IActionResult FirstPage()
		{
			return View();
		}

		public IActionResult About()
		{
			return View();
		}
		public IActionResult Services()
		{
			return View();
		}

		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Login(LoginViewModel model)
		{
			await CheckForOverduePaymentsAndApplyPenalties();

			var user=await _userManager.FindByNameAsync(model.TC);

			if (user != null && await _userManager.IsInRoleAsync(user, "User") && user.WBandState==false)
			{
				ModelState.AddModelError(string.Empty, "Şu an hesabınız aktif değil. Lütfen kuruma gelerek hesabınızı aktive ediniz.");
				TempData["ErrorMessage"] = "Şu an hesabınız aktif değil.. Lütfen kuruma gelerek hesabınızı aktive ediniz.";
				return View(model);
			}

			// 1. ModelState doğrulamasını kontrol et
			if (!ModelState.IsValid)
			{
				// ModelState geçerli değilse, doğrulama hatalarını görüntüye geri gönder
				TempData["ErrorMessage"] = "Lütfen tüm gerekli alanları doldurunuz veya geçerli bilgiler giriniz.";
				return View(model);
			}

			if (user != null && user.IsBlocked)
			{
				ModelState.AddModelError(string.Empty, "Hesabınız 2 ceza puanına ulaştığı için bloke edilmiştir. Lütfen yetkililerle iletişime geçiniz.");
				return View(model);
			}

			if (user == null)
			{
				ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre yanlış.");
				TempData["ErrorMessage"] = "Kullanıcı adı veya şifre yanlış.";
				return View(model);
			}

			// 4. Şifre kontrolü yap
			var result = await _signInManager.PasswordSignInAsync(
				user.UserName,
				model.Password,
				model.RememberMe,
				lockoutOnFailure: false); // Hesap kilitleme politikasını burada kontrol edebilirsiniz.

			if (result.Succeeded)
			{
				// 5. Giriş başarılı, kullanıcının rollerini al
				var roles = await _userManager.GetRolesAsync(user);

				// 6. Rolüne göre yönlendir
				if (roles.Contains("Admin"))
				{
					return RedirectToAction("AdminPan", "Admin");
				}
				else if(roles.Contains("User"))
				{
					return RedirectToAction("MainPage", "User");
				}
			}
			else if (result.IsLockedOut)
			{
				// Hesap kilitlendiyse
				ModelState.AddModelError(string.Empty, "Hesabınız kilitlenmiştir. Lütfen daha sonra tekrar deneyin veya yöneticinizle iletişime geçin.");
				TempData["ErrorMessage"] = "Hesabınız kilitlenmiştir. Lütfen daha sonra tekrar deneyin veya yöneticinizle iletişime geçin.";
			}
			else
			{
				// Diğer başarısız giriş denemeleri (genellikle yanlış şifre)
				ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre yanlış.");
				TempData["ErrorMessage"] = "Kullanıcı adı veya şifre yanlış.";
			}

			// 7. Tüm hata senaryolarında, hata mesajıyla birlikte View'i geri dön
			return View(model);
		}

		// --- YENİ ÖZEL METOT: Arka planda ceza sistemini çalıştırır ---
		private async Task CheckForOverduePaymentsAndApplyPenalties()
		{
			// Son ödeme tarihi geçmiş ve ödemesi yapılmamış tüm başvuruları bul.
			var overdueRequests = await _context.ApplicationRequests
				.Include(ar => ar.Session.Service)
				.Include(ar => ar.ApplicationUser)
				.Where(ar => !ar.PaymentComplete && ar.Session.Service.PayDueDate < DateTime.Today)
				.ToListAsync();

			if (!overdueRequests.Any()) return; // Ceza verilecek kimse yoksa çık.

			var usersToPenalize = overdueRequests.GroupBy(ar => ar.ApplicationUser).ToList();

			foreach (var userGroup in usersToPenalize)
			{
				var user = userGroup.Key;
				var serviceTitles = string.Join(", ", userGroup.Select(g => g.Session.Service.Title).Distinct());

				_context.UserPenalties.Add(new UserPenalty
				{
					UserId = user.Id,
					SessionId = userGroup.First().SessionId,
					Reason = $"Son ödeme tarihi geçen '{serviceTitles}' hizmeti için ödeme yapılmadı."
				});

				var totalPenalties = await _context.UserPenalties.CountAsync(p => p.UserId == user.Id) + 1;

				if (totalPenalties >= 2 && !user.IsBlocked)
				{
					user.IsBlocked = true;

					var subject = "Hesabınız Bloke Edilmiştir";
					var body = $"<p>Sayın {user.Name} {user.Surname},</p><p>Sistemimizde 2 veya daha fazla ceza puanına ulaştığınız için hesabınız bloke edilmiştir.</p>";
					await _emailService.SendEmailAsync(user.Email, subject, body);
				}
			}
			_context.ApplicationRequests.RemoveRange(overdueRequests);
			await _context.SaveChangesAsync();
		}

		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Register(RegisterViewModel model)
		{

			if (ModelState.IsValid)
			{

				//Rastgele şifre oluştur
				var randomPassword = GenerateRandomPassword(12);

				// Kullanıcı oluştur
				var user = new ApplicationUser
				{
					UserName = model.TC, // TC'yi kullanıcı adı olarak kullan
					Email = model.Email,
					Name = model.Name,
					Gender = model.Gender,
					UserType = UserType.Bilinmiyor,
					Surname = model.Surname,
					PhoneNumber = model.Phone,
				};

				// Kullanıcıyı oluştur  ve rastgele oluşturulmuş şifreyi kullanıcıya ata.
				var result = await _userManager.CreateAsync(user, randomPassword);

				if (result.Succeeded)
				{

					// Rol ataması yap
					string roleName = "User"; 
					await _userManager.AddToRoleAsync(user, roleName);

					//Şifreyi mail ile kullanıcıya gönder.
					await _emailService.SendPasswordEmail(user.Email, randomPassword);

					TempData["RegisterSuccess"] = "Ön Kaydınız başarıyla oluşturuldu. Şifreniz mail ile gönderildi. Spor müdürlüğüne" +
						"giderek hesabınızı kullanıma açtırabilirsiniz.";
					return RedirectToAction("Register");

				}

				// Hataları ekle
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}

			return View(model);
		}

		private string GenerateRandomPassword(int length)
		{
			if (length < 6) length = 6; // minimum güvenlik

			const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			const string lower = "abcdefghijklmnopqrstuvwxyz";
			const string digits = "0123456789";
			const string special = "!@#$%^&*<>,;.:?-_/()[]{}";
			const string allChars = upper + lower + digits + special;

			var random = new Random();
			var passwordChars = new List<char>();

			// Her karakter grubundan en az bir tane ekle
			passwordChars.Add(upper[random.Next(upper.Length)]);
			passwordChars.Add(lower[random.Next(lower.Length)]);
			passwordChars.Add(digits[random.Next(digits.Length)]);
			passwordChars.Add(special[random.Next(special.Length)]);

			// Geri kalan karakterleri rastgele doldur
			for (int i = passwordChars.Count; i < length; i++)
			{
				passwordChars.Add(allChars[random.Next(allChars.Length)]);
			}

			// Karakterleri karıştır
			return new string(passwordChars.OrderBy(x => random.Next()).ToArray());
		}

		
	}
}