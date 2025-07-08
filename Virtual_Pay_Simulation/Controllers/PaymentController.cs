using Microsoft.AspNetCore.Mvc;
using Virtual_Pay_Simulation.Models;
using System.Linq;
using Virtual_Pay_Simulation.Data;
using Microsoft.AspNetCore.Diagnostics;

namespace Virtual_Pay_Simulation.Controllers
{
	public class PaymentController : Controller
	{
		private readonly AppDbContext _context; //bu context bağlantısı

		public PaymentController(AppDbContext context) //Veritabanı bağlantısı alır
		{
			_context = context;
		}

		// Ana Hoşgeldiniz sayfası
		[HttpGet]
		public IActionResult FirstPage()
		{
			return View();
		}


		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		private string HashPassword(string password)
		{
			using (var sha256 = System.Security.Cryptography.SHA256.Create())
			{
				byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
				byte[] hashBytes = sha256.ComputeHash(bytes);
				return Convert.ToBase64String(hashBytes);
			}
		}


		[HttpPost]
		public IActionResult Register(string ad, string soyad, string tc, string sifre)
		{
			if (string.IsNullOrWhiteSpace(tc))
			{
				ViewBag.RegisterError = "TC alanı boş bırakılamaz!";
				return View();
			}

			tc = tc.Trim();

			if (!long.TryParse(tc, out long parsedTc))
			{
				ViewBag.RegisterError = "TC sayısal olmalı ve yalnızca rakamlardan oluşmalı!";
				return View();
			}

			if (tc.Length != 11)
			{
				ViewBag.RegisterError = "TC kimlik numarası 11 haneli olmalı!";
				return View();
			}


			var mevcutUye = _context.Uyeler.FirstOrDefault(u => u.TC == parsedTc);
			if (mevcutUye != null)
			{
				ViewBag.RegisterError = "Bu kullanıcı zaten kayıtlı!";
				return View();
			}

			// HASHLEME
			var hashedPassword = HashPassword(sifre);

			var yeniUye = new Uye
			{
				Name = ad,
				Surname = soyad,
				TC = parsedTc,
				Password = hashedPassword,
				WBandState = false
			};

			_context.Uyeler.Add(yeniUye);
			_context.SaveChanges();

			ViewBag.RegisterSuccess = "Kaydınız başarıyla tamamlandı!";
			return View();
		}

		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
		public IActionResult Login(string tc, string sifre)
		{
			tc = tc?.Trim();

			if (string.IsNullOrWhiteSpace(tc))
			{
				ViewBag.LoginError = "TC alanı boş bırakılamaz!";
				return View();
			}

			if (!long.TryParse(tc, out long parsedTc))
			{
				ViewBag.LoginError = "TC sayısal olmalı ve yalnızca rakamlardan oluşmalı!";
				return View();
			}

			if (tc.Length != 11)
			{
				ViewBag.LoginError = "TC kimlik numarası 11 haneli olmalı!";
				return View();
			}

			// ✅ Girilen şifreyi hashle
			var hashedPassword = HashPassword(sifre);

			// ✅ Hashli haliyle karşılaştır
			var uye = _context.Uyeler.FirstOrDefault(u => u.TC == parsedTc && u.Password == hashedPassword);

			if (uye != null)
			{
				HttpContext.Session.SetString("UserTC", parsedTc.ToString());
				return RedirectToAction("MainPage");
			}
			else
			{
				ViewBag.LoginError = "TC veya şifre hatalı!";
				return View();
			}
		}


		// Ödeme ekranı
		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		[ActionName("Create")]
		public IActionResult CreateForm(string Ad, string Soyad, string TC, string KartNo, string SonKullanmaTarihi, string CVC)
		{
			// Kullanıcı bilgileri kontrolü
			if (string.IsNullOrWhiteSpace(TC) || !long.TryParse(TC.Trim(), out long parsedTc) || TC.Trim().Length != 11)
			{
				ViewBag.Mesaj = "TC kimlik numarası 11 haneli sayısal olmalıdır!";
				ViewBag.UyeVar = false;
				ViewBag.Ad = Ad;
				ViewBag.Soyad = Soyad;
				ViewBag.TC = TC;
				return View("Create");
			}

			// Kullanıcıyı ara
			var uye = _context.Uyeler.FirstOrDefault(u => u.TC == parsedTc && u.Name == Ad && u.Surname == Soyad);

			if (uye == null)
			{
				// Kullanıcı yok → kırmızı uyarı, kart alanı gizli
				ViewBag.Mesaj = "Böyle bir kullanıcı bulunamadı.";
				ViewBag.UyeVar = false;
				ViewBag.Ad = Ad;
				ViewBag.Soyad = Soyad;
				ViewBag.TC = TC;
				return View("Create");
			}

			// Kullanıcı bulundu → kart bilgileri soruluyor
			if (string.IsNullOrWhiteSpace(KartNo) || string.IsNullOrWhiteSpace(SonKullanmaTarihi) || string.IsNullOrWhiteSpace(CVC))
			{
				// Kart alanı henüz doldurulmadı → sadece kart kısmını göster
				ViewBag.Mesaj = "Kullanıcı bulundu.";
				ViewBag.UyeVar = true;
				ViewBag.Ad = Ad;
				ViewBag.Soyad = Soyad;
				ViewBag.TC = TC;
				return View("Create");
			}

			// Kart bilgileri doldu → ödeme adımına geç
			return RedirectToAction("Confirm");
		}


		[HttpGet]
		public IActionResult Confirm()
		{
			Random rnd = new Random();
			int code = rnd.Next(100000, 999999);
			ViewBag.RandomCode = code;
			return View();
		}

		[HttpPost]
		public IActionResult ConfirmSubmit(string generatedCode, string userCode)
		{
			if (generatedCode == userCode)
			{
				// Kod doğru → ödeme başarılı
				return RedirectToAction("Success");
			}
			else
			{
				// Kod yanlış → aynı sayfada hata mesajı göster
				ViewBag.ErrorMessage = "Hatalı kod girdiniz. Lütfen tekrar deneyin.";
				ViewBag.RandomCode = generatedCode;
				return View("Confirm");
			}
		}

		[HttpGet]
		public IActionResult MainPage()
		{
			var userTcString = HttpContext.Session.GetString("UserTC");

			if (string.IsNullOrEmpty(userTcString))
			{
				// Session yoksa login sayfasına yönlendir
				return RedirectToAction("Login");
			}

			if (!long.TryParse(userTcString, out long userTc))
			{
				return RedirectToAction("Login");
			}

			var uye = _context.Uyeler.FirstOrDefault(u => u.TC == userTc);
			if (uye == null)
			{
				return RedirectToAction("Login");
			}

			ViewBag.UserName = uye.Name;
			ViewBag.UserSurname = uye.Surname;

			return View();
		}

		[HttpGet]
		public IActionResult Success()
		{
			return View();
		}

	}
}