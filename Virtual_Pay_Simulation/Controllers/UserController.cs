using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Virtual_Pay_Simulation.Data;
using Virtual_Pay_Simulation.Extensions;
using Virtual_Pay_Simulation.Interfaces;
using Virtual_Pay_Simulation.Models;
using Virtual_Pay_Simulation.Models.ViewModels;



namespace Virtual_Pay_Simulation.Controllers
{
	[Authorize(Roles = "User")] // Sadece User rolüne sahip kullanıcılar erişebilir
	public class UserController : Controller
	{
		private readonly AppDbContext _context; //bu context bağlantısı  
		private readonly IEmailService _emailService; // EmailService bağı  
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly ICompositeViewEngine _viewEngine;

		public UserController(AppDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ICompositeViewEngine viewEngine) // Veritabanı bağlantısını alır
		{
			_context = context;
			_emailService = new EmailService(); // EmailService nesnesi
			_userManager = userManager;
			_signInManager = signInManager;
			_viewEngine = viewEngine;
		}

		[HttpGet]
		public IActionResult Services(string status)
		{
			var services = _context.Services.AsQueryable();

			if (status == "active")
			{
				services = services.Where(s => s.IsActive);
			}

			return View(services.ToList());
		}

		[HttpGet]
		public async Task<IActionResult> Profile()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return Challenge();
			}

			var viewModel = new ProfileViewModel
			{
				FullName = $"{user.Name} {user.Surname}",
				TCKimlikNo = user.UserName,
				Email = user.Email,
				PhoneNumber = user.PhoneNumber,
				UserType = user.UserType.ToString(),
				Gender = user.Gender.ToString(),
				WristBand = user.WristBand,
				PenaltyCount = await _context.UserPenalties.CountAsync(p => p.UserId == user.Id)
			};

			return View(viewModel);
		}

		[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
		[HttpGet]
		public async Task<IActionResult> ServiceInfo(int id)
		{
			var service = await _context.Services.FirstOrDefaultAsync(s => s.ServiceId == id && s.IsActive);
			if (service == null) return NotFound();

			var userId = _userManager.GetUserId(User);
			var applicationUser = await _userManager.FindByIdAsync(userId);
			var userType = applicationUser?.UserType ?? UserType.Bilinmiyor;
			var userGender = applicationUser?.Gender;

			var userAppliedSessionIds = await _context.ApplicationRequests
				.Where(ar => ar.UserId == userId && ar.Session.ServiceId == id && !ar.PaymentComplete)
				.Select(ar => ar.SessionId)
				.ToListAsync();

			var userPaidSessionIds = await _context.ApplicationRequests
				.Where(ar => ar.UserId == userId && ar.Session.ServiceId == id && ar.PaymentComplete)
				.Select(ar => ar.SessionId)
				.ToListAsync();

			// --- YENİ EKLENEN KONTROL ---
			// Kullanıcının bu hizmetten daha önce herhangi bir seans satın alıp almadığını kontrol et.
			bool userHasPurchasedInService = await _context.ApplicationRequests
				.Include(ar => ar.Session)
				.AnyAsync(ar => ar.UserId == userId && ar.Session.ServiceId == id && ar.PaymentComplete);
			// --- KONTROL SONU ---

			var seanceGroups = (await _context.Sessions
				.Where(s => s.ServiceId == id && s.ServiceDay.ServiceDayTemplate.Gender == userGender)
				.Include(s => s.ServiceDay)
					.ThenInclude(sd => sd.ServiceDayTemplate)
				.Include(s => s.ApplicationRequests)
				.ToListAsync())
				.GroupBy(s => new
				{
					TemplateId = s.ServiceDay.TemplateId,
					TemplateName = s.ServiceDay.ServiceDayTemplate.TemplateName,
					Gender = s.ServiceDay.ServiceDayTemplate.Gender,
					StartTime = s.StartTime,
					EndTime = s.EndTime,
					MinCapacity = s.MinCapacity,
					BorderCapacity = s.BorderCapacity,
					MaxCapacity = s.MaxCapacity
				})
				.Select(g => new SeanceGroupModel
				{
					TemplateName = g.Key.TemplateName,
					Gender = g.Key.Gender,
					StartTime = g.Key.StartTime,
					EndTime = g.Key.EndTime,
					MinCapacity = g.Key.MinCapacity,
					// --- GÜNCELLEME: BorderCapacity'yi de ViewModel'e ekliyoruz ---
					BorderCapacity = g.Key.BorderCapacity,
					MaxCapacity = g.Key.MaxCapacity,
					CurrentCapacity = g.SelectMany(s => s.ApplicationRequests.Where(ar => ar.PaymentComplete)).Select(ar => ar.UserId).Distinct().Count(),
					ApplicantCount = g.SelectMany(s => s.ApplicationRequests.Where(ar => !ar.PaymentComplete)).Select(ar => ar.UserId).Distinct().Count(),
					Days = g.Select(s => s.ServiceDay.Day.ToString()).Distinct().ToList(),
					SessionIds = g.Select(s => s.SessionId).ToList()
				})
				.ToList();

			var viewModel = new UserServiceInfoViewModel
			{
				Title = service.Title,
				PayDueDate = service.PayDueDate,
				SeanceGroups = seanceGroups,
				UserAppliedSessionIds = userAppliedSessionIds,
				UserPaidSessionIds = userPaidSessionIds,
				Service = service,
				UserType = userType,
				// --- GÜNCELLEME: Hesaplanan yeni bilgiyi ViewModel'e ata ---
				UserHasPurchasedInService = userHasPurchasedInService
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> ApplyOrPurchase(string sessionIds, bool isRequest)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var ids = sessionIds.Split(',').Select(int.Parse).ToList();
			if (!ids.Any()) return BadRequest("Seans ID'leri bulunamadı.");

			var firstSession = await _context.Sessions.Include(s => s.Service).FirstOrDefaultAsync(s => s.SessionId == ids.First());
			if (firstSession == null) return NotFound("Seans bulunamadı.");
			var serviceId = firstSession.ServiceId;

			// --- YENİ KONTROL MANTIĞI ---

			// Önce sınır kapasite kontrolünü yapalım (sadece başka seans almışlar için geçerli)
			bool hasPurchasedOtherSessionInService = await _context.ApplicationRequests
				.Include(ar => ar.Session)
				.AnyAsync(ar => ar.UserId == userId && ar.Session.ServiceId == serviceId && ar.PaymentComplete);

			if (hasPurchasedOtherSessionInService)
			{
				var newGroupCurrentCapacity = await _context.ApplicationRequests
					.Where(ar => ids.Contains(ar.SessionId) && ar.PaymentComplete)
					.Select(ar => ar.UserId)
					.Distinct()
					.CountAsync();

				if (newGroupCurrentCapacity >= firstSession.BorderCapacity)
				{
					TempData["Error"] = "Bu seans, hizmetten daha önce alım yapmış kullanıcılar için sınır kapasiteyi doldurmuştur.";
					return RedirectToAction("ServiceInfo", new { id = serviceId });
				}
			}

			// Şimdi, kullanıcının bu spesifik gruba yaptığı mevcut başvuruyu bulalım
			var existingRequests = await _context.ApplicationRequests
				.Where(ar => ar.UserId == userId && ids.Contains(ar.SessionId))
				.ToListAsync();

			if (isRequest) // KULLANICI "BAŞVUR" BUTONUNA BASTI
			{
				if (existingRequests.Any())
				{
					TempData["Error"] = "Bu seans grubuna zaten başvurdunuz veya satın aldınız.";
					return RedirectToAction("ServiceInfo", new { id = serviceId });
				}

				// Yeni başvuru oluştur
				foreach (var sessionId in ids)
				{
					var application = new ApplicationRequest
					{
						SessionId = sessionId,
						UserId = userId,
						IsRequest = true,
						PaymentComplete = false,
						EnrollmentStatus = EnrollmentStatus.Ödenmedi,
						RequestDate = DateTime.Now
					};
					_context.ApplicationRequests.Add(application);
				}
				await _context.SaveChangesAsync();

				// burada e posta gönderiliyor başvuru yapılmış seans açıldığında ödeme yapsınlar diye.
				var sessionsInGroup = await _context.Sessions
					.Where(s => ids.Contains(s.SessionId))
					.Include(s => s.ApplicationRequests)
					.Include(s => s.Service)
					.Include(s => s.ServiceDay)
					.ToListAsync();

				var minCapacity = sessionsInGroup.First().MinCapacity;
				var totalApplicants = sessionsInGroup
					.SelectMany(s => s.ApplicationRequests.Where(ar => !ar.PaymentComplete))
					.Select(ar => ar.UserId)
					.Distinct()
					.Count();

				if (totalApplicants >= minCapacity)
				{
					await SendCapacityReachedEmail(sessionsInGroup);
				}

				TempData["Success"] = "Başvurunuz başarıyla alındı.";
			}
			else // Kullanıcı "Satın Al" butonuna bastı
			{
				// Artık doğrudan ödeme sayfasına gitmek yerine, sözleşme kontrolü yapan yeni metodumuzu çağırıyoruz.
				return RedirectToAction("CheckAndInitiatePurchase", new { sessionIds });
			}

			return RedirectToAction("ServiceInfo", new { id = serviceId });
		}

		// --- YENİ METOD: Ödemeyi İptal Etme ---
		[HttpGet]
		public async Task<IActionResult> CancelPayment(string sessionIds, int serviceId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var ids = sessionIds.Split(',').Select(int.Parse).ToList();

			// Kullanıcının "Ödemede" durumundaki kayıtlarını bul
			var requestsToCancel = await _context.ApplicationRequests
				.Where(ar => ar.UserId == userId && ids.Contains(ar.SessionId) && ar.EnrollmentStatus == EnrollmentStatus.Ödemede)
				.ToListAsync();

			if (requestsToCancel.Any())
			{
				_context.ApplicationRequests.RemoveRange(requestsToCancel);
				await _context.SaveChangesAsync();
				TempData["Success"] = "Ödeme işlemi başarıyla iptal edildi.";
			}

			return RedirectToAction("ServiceInfo", new { id = serviceId });
		}

		private async Task SendCapacityReachedEmail(List<Session> sessions)
		{
			if (!sessions.Any()) return;

			var sessionIds = sessions.Select(s => s.SessionId).ToList();
			var firstSession = sessions.First();
			var service = firstSession.Service;

			// --- YENİ: Seans gün ve saat bilgilerini al ---
			var startTime = firstSession.StartTime.ToString(@"hh\:mm");
			var endTime = firstSession.EndTime.ToString(@"hh\:mm");

			// Gün isimlerini Türkçe almak için CultureInfo kullanıyoruz.
			var culture = new CultureInfo("tr-TR");
			var daysOfWeek = sessions
				.Select(s => culture.DateTimeFormat.GetDayName(s.ServiceDay.Day))
				.Distinct()
				.ToList();
			var daysString = string.Join(", ", daysOfWeek);
			// --- YENİ BÖLÜM SONU ---

			var applicants = await _context.ApplicationRequests
				.Where(ar => sessionIds.Contains(ar.SessionId) && ar.IsRequest && !ar.PaymentComplete)
				.Include(ar => ar.ApplicationUser)
				.Select(ar => ar.ApplicationUser)
				.Distinct()
				.ToListAsync();

			var subject = $"{service.Title} Hizmeti İçin Ödeme Zamanı";

			foreach (var user in applicants)
			{
				// --- YENİ: E-posta metni güncellendi ---
				var body = $@"
                    <p>Sayın {user.Name} {user.Surname},</p>
                    <p>Başvuru yaptığınız '{service.Title}' hizmeti, yeterli başvuru sayısına ulaştığı için satın alıma açılmıştır.</p>
                    
                    <div style='border: 1px solid #ddd; padding: 10px; margin: 15px 0; background-color: #f9f9f9;'>
                        <h4 style='margin-top: 0;'>Seans Detayları</h4>
                        <p><strong>Saat:</strong> {startTime} - {endTime}</p>
                        <p><strong>Günler:</strong> {daysString}</p>
                    </div>

                    <p><b>{service.PayDueDate:dd.MM.yyyy}</b> tarihine kadar seansın ödemesini yapmanız rica olunur.</p>
                    <p>Aksi takdirde ceza puanı alacaksınız. Üç ceza puanında hesabınız bloke edilir.</p>
                    <p>İyi günler dileriz.</p>";

				await _emailService.SendEmailAsync(user.Email, subject, body);
			}
		}

		[HttpGet]
		public async Task<IActionResult> CancelApplication(string sessionIds)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var ids = sessionIds.Split(',').Select(int.Parse).ToList();

			var requestsToCancel = await _context.ApplicationRequests
				.Where(ar => ar.UserId == userId && ids.Contains(ar.SessionId) && !ar.PaymentComplete)
				.ToListAsync();

			var serviceId = await _context.Sessions
				.Where(s => ids.Contains(s.SessionId))
				.Select(s => s.ServiceId)
				.FirstOrDefaultAsync();

			if (requestsToCancel.Any())
			{
				_context.ApplicationRequests.RemoveRange(requestsToCancel);
				await _context.SaveChangesAsync();
				TempData["Success"] = "Başvurunuz başarıyla iptal edilmiştir.";
			}
			else
			{
				TempData["Error"] = "İptal edilecek bir başvuru bulunamadı.";
			}

			return RedirectToAction("ServiceInfo", new { id = serviceId });
		}

		[HttpPost]
		public async Task<IActionResult> Create(string sessionIds, decimal price, string kartNo, string sonKullanmaTarihi, string cvc)
		{
			bool paymentSuccessful = true; // Ödeme simülasyonu

			if (paymentSuccessful)
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(userId)) return Unauthorized();

				var ids = sessionIds.Split(',').Select(int.Parse).ToList();
				if (!ids.Any()) return BadRequest("Seans ID'leri bulunamadı.");

				// Adım 1: "Ödemede" durumundaki kayıtları bul
				var requestsToUpdate = await _context.ApplicationRequests
					.Include(ar => ar.Session) // ServiceId'yi alabilmek için Session'ı dahil et
					.Where(ar => ar.UserId == userId && ids.Contains(ar.SessionId) && ar.EnrollmentStatus == EnrollmentStatus.Ödemede)
					.ToListAsync();

				if (!requestsToUpdate.Any())
				{
					TempData["Error"] = "Ödeme yapılacak geçerli bir seans bulunamadı veya işlem zaman aşımına uğradı.";
					return RedirectToAction("Services");
				}

				// Adım 2: ApplicationRequest kayıtlarını "Ödendi" olarak güncelle
				foreach (var request in requestsToUpdate)
				{
					request.PaymentComplete = true;
					request.EnrollmentStatus = EnrollmentStatus.Ödendi;
				}

				// Adım 3: Purchases tablosuna özet kaydını oluştur
				var purchase = new Purchase
				{
					UserId = userId,
					PaidAmount = price,
					PurchaseDate = DateTime.Now,
					SessionIds = sessionIds
				};
				_context.Purchases.Add(purchase);

				// Adım 4: Attendances (yoklama) kayıtlarını oluştur
				var firstSessionInGroup = requestsToUpdate.First().Session;
				var service = await _context.Services.FindAsync(firstSessionInGroup.ServiceId);

				if (service == null)
				{
					TempData["Error"] = "Hizmet bilgileri alınamadı. Kayıt tamamlanamadı.";
					return RedirectToAction("Services");
				}

				var serviceStartDate = service.StartTime;
				var serviceEndDate = service.EndTime;

				var sessionsInGroup = await _context.Sessions
					.Where(s => ids.Contains(s.SessionId))
					.Include(s => s.ServiceDay)
					.ToListAsync();
				var serviceDaysOfWeek = sessionsInGroup.Select(s => s.ServiceDay.Day).Distinct().ToList();

				for (var date = serviceStartDate.Date; date <= serviceEndDate.Date; date = date.AddDays(1))
				{
					if (serviceDaysOfWeek.Contains(date.DayOfWeek))
					{
						var sessionForThisDay = sessionsInGroup.FirstOrDefault(s => s.ServiceDay.Day == date.DayOfWeek);
						if (sessionForThisDay != null)
						{
							var attendance = new Attendance
							{
								UserId = userId,
								SessionId = sessionForThisDay.SessionId,
								IsPresent = false,
								AttendanceDate = date,
								StartTime = sessionForThisDay.StartTime,
								EndTime = sessionForThisDay.EndTime
							};
							_context.Attendaces.Add(attendance);
						}
					}
				}

				// Adım 5: Tüm değişiklikleri (ApplicationRequest, Purchase, Attendance) veritabanına kaydet
				await _context.SaveChangesAsync();

				TempData["Success"] = "Ödemeniz başarıyla tamamlandı ve seanslarınız oluşturuldu.";
				return RedirectToAction("Confirm", "User");
			}

			TempData["Error"] = "Ödeme işlemi başarısız oldu. Lütfen bilgilerinizi kontrol ediniz.";
			return RedirectToAction("PayCreate", new { sessionIds, price });
		}

		[HttpGet]
		public async Task<IActionResult> PayCreate(string sessionIds, decimal price, int serviceId)
		{
			var userId = _userManager.GetUserId(User);
			var applicationUser = await _userManager.FindByIdAsync(userId);

			var firstSessionId = sessionIds.Split(',').Select(int.Parse).FirstOrDefault();
			var serviceTitle = _context.Sessions
							   .Include(s => s.Service)
							   .FirstOrDefault(s => s.SessionId == firstSessionId)?
							   .Service.Title;

			var viewModel = new PayCreateViewModel
			{
				User = applicationUser,
				PaidAmount = price,
				SessionIds = sessionIds,
				ServiceTitle = serviceTitle,
				ServiceId = serviceId
			};

			// Onay mesajını ViewBag ile gönder
			ViewBag.ConfirmationMessage = "Bu seansı satın almak istediğinizden emin misiniz?";

			return View(viewModel);
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
		public async Task <IActionResult> MainPage()
		{
			//Kullanıcıyı identity sisteminden alıyoruz.
			var user = await _userManager.GetUserAsync(User);
			
			if(user==null)
			{
				await _signInManager.SignOutAsync();
				return RedirectToAction("Login", "Public");
			}
			//Wiewe kullanıcı bilgilerini gönderiyoruz.
			ViewBag.UserName = user.Name;
			ViewBag.UserSurname = user.Surname;

			return View();
		}

		[HttpGet]
		public IActionResult Success()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> CurrentSessions()
		{
			var userId = _userManager.GetUserId(User);
			var culture = new CultureInfo("tr-TR");

			var purchasedGroups = new List<SessionGroupViewModel>();
			var pastGroups = new List<SessionGroupViewModel>();
			var appliedGroups = new List<SessionGroupViewModel>();

			// 1. Ödemesi yapılmış seansları "Purchase" üzerinden grupla
			var userPurchases = await _context.Purchases
				.Where(p => p.UserId == userId)
				.OrderByDescending(p => p.PurchaseDate)
				.ToListAsync();

			foreach (var purchase in userPurchases)
			{
				var sessionIdsInPurchase = purchase.SessionIds.Split(',').Select(int.Parse).ToList();
				if (!sessionIdsInPurchase.Any()) continue;

				var firstSession = await _context.Sessions
					.Include(s => s.Service)
					.Include(s => s.ServiceDay)
					.FirstOrDefaultAsync(s => s.SessionId == sessionIdsInPurchase.First());

				if (firstSession == null) continue;

				var service = firstSession.Service;
				var allDaysInGroup = await _context.Sessions
					.Where(s => sessionIdsInPurchase.Contains(s.SessionId))
					.Select(s => culture.DateTimeFormat.GetDayName(s.ServiceDay.Day))
					.Distinct()
					.ToListAsync();

				var days = string.Join(", ", allDaysInGroup);

				var groupDetails = new SessionGroupViewModel
				{
					PurchaseId = purchase.PurchaseId,
					ServiceTitle = service.Title,
					ServiceStartDate = service.StartTime,
					ServiceEndDate = service.EndTime,
					StartTime = firstSession.StartTime,
					EndTime = firstSession.EndTime,
					Days = days
				};

				if (service.EndTime.Date >= DateTime.Today)
				{
					purchasedGroups.Add(groupDetails);
				}
				else
				{
					pastGroups.Add(groupDetails);
				}
			}

			// 2. Başvuruları kalıplara göre grupla
			var userApplications = await _context.ApplicationRequests
				.Where(ar => ar.UserId == userId && !ar.PaymentComplete)
				.Include(ar => ar.Session).ThenInclude(s => s.Service)
				.Include(ar => ar.Session).ThenInclude(s => s.ServiceDay).ThenInclude(sd => sd.ServiceDayTemplate)
				.ToListAsync();

			var groupedApplications = userApplications
				.GroupBy(ar => new { ar.Session.ServiceId, ar.Session.StartTime, ar.Session.ServiceDay.TemplateId })
				.ToList();

			foreach (var group in groupedApplications)
			{
				var firstRequest = group.First();
				var session = firstRequest.Session;
				var service = session.Service;
				var days = string.Join(", ", group.Select(g => culture.DateTimeFormat.GetDayName(g.Session.ServiceDay.Day)).Distinct());

				appliedGroups.Add(new SessionGroupViewModel
				{
					ServiceTitle = service.Title,
					ServiceStartDate = service.StartTime,
					ServiceEndDate = service.EndTime,
					StartTime = session.StartTime,
					EndTime = session.EndTime,
					Days = days
				});
			}

			var viewModel = new UserSessionsViewModel
			{
				AppliedSessions = appliedGroups,
				PurchasedSessions = purchasedGroups,
				PastSessions = pastGroups
			};

			return View(viewModel);
		}

		// --- YENİ METOD: Yoklama Detay Sayfası ---
		[HttpGet]
		public async Task<IActionResult> SessionAttendance(int purchaseId)
		{
			var userId = _userManager.GetUserId(User);
			var culture = new CultureInfo("tr-TR");
			var purchase = await _context.Purchases
				.FirstOrDefaultAsync(p => p.PurchaseId == purchaseId && p.UserId == userId);

			if (purchase == null) return NotFound();

			var sessionIdsInPurchase = purchase.SessionIds.Split(',').Select(int.Parse).ToList();

			var firstSession = await _context.Sessions
				.Include(s => s.Service)
				.Include(s => s.ServiceDay)
				.FirstOrDefaultAsync(s => sessionIdsInPurchase.Contains(s.SessionId));

			if (firstSession == null) return NotFound();

			var allDaysInGroup = await _context.Sessions
				.Where(s => sessionIdsInPurchase.Contains(s.SessionId))
				.Select(s => culture.DateTimeFormat.GetDayName(s.ServiceDay.Day))
				.Distinct()
				.ToListAsync();

			var attendanceRecords = await _context.Attendaces
				.Where(a => a.UserId == userId && sessionIdsInPurchase.Contains(a.SessionId))
				.OrderBy(a => a.AttendanceDate)
				.ToListAsync();

			var viewModel = new SessionAttendanceViewModel
			{
				ServiceTitle = firstSession.Service.Title,
				SessionTime = $"{firstSession.StartTime:hh\\:mm} - {firstSession.EndTime:hh\\:mm}",
				SessionDays = string.Join(", ", allDaysInGroup),
				AttendanceRecords = attendanceRecords
			};

			return View(viewModel);
		}

		// --- SÖZLEŞME AKIŞI İÇİN EKLENEN 3 YENİ METOT ---

		// 1. "Satın Al" butonuna basıldığında İLK ÇALIŞAN METOT
		[HttpGet]
		public async Task<IActionResult> CheckAndInitiatePurchase(string sessionIds)
		{
			var ids = sessionIds.Split(',').Select(int.Parse).ToList();
			var firstSession = await _context.Sessions
				.Include(s => s.Service)
				.FirstOrDefaultAsync(s => s.SessionId == ids.First());

			if (firstSession != null && firstSession.Service.PayDueDate < DateTime.Today)
			{
				TempData["Error"] = "Bu hizmet için satın alım süresi sona ermiştir.";
				return RedirectToAction("ServiceInfo", new { id = firstSession.ServiceId });
			}

			var user = await _userManager.GetUserAsync(User);
			var activeContract = await _context.Contracts.FirstOrDefaultAsync(c => c.IsActive);

			// Eğer aktif bir sözleşme yoksa veya kullanıcı zaten son versiyonu kabul etmişse, doğrudan ödemeye yönlendir.
			if (activeContract == null || user.AcceptedContractVersion == activeContract.Version)
			{
				return Json(new { action ="redirect", url=Url.Action("InitiatePurchase", "User", new {sessionIds}) });
			}

			// Kullanıcı sözleşmeyi kabul etmemişse, sözleşme modal'ını göster.
			var contractViewModel = new ContractViewModel
			{
				Title = activeContract.Title,
				Content = activeContract.Content,
				Version = activeContract.Version,
				SessionIds = sessionIds
			};

			return Json(new { action = "showModal", html = await this.RenderViewToStringAsync("_ContractModalPartial", contractViewModel) });
		}

		// Sözleşmeyi imzalamış kullanıcıların yönlendirme yardımcı metodu
		private async Task<string> RenderViewToStringAsync(string viewName, object model)
		{
			ViewData.Model = model;
			using(var sw=new StringWriter())
			{
				var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);

				if(viewResult.View==null)
				{
					throw new ArgumentNullException($"{viewName} does not match any available view");
				}

				var viewContext = new ViewContext(
					ControllerContext,
					viewResult.View,
					ViewData,
					TempData,
					sw,
					new HtmlHelperOptions()
					);

				await viewResult.View.RenderAsync(viewContext);

				return sw.ToString();
			}
		}

		// 2. Kullanıcı sözleşmeyi ONAYLADIĞINDA çalışan metot
		[HttpPost]
		public async Task<IActionResult> AcceptContract(int version)
		{
			var user = await _userManager.GetUserAsync(User);
			user.AcceptedContractVersion = version;
			user.ContractAccepptanceDate = DateTime.Now;

			var result = await _userManager.UpdateAsync(user);

			if (result.Succeeded)
			{
				return Json(new { success = true });
			}
			return Json(new { success = false, message = "Onayınız kaydedilirken bir hata oluştu." });
		}

		// 3. Sözleşme onaylandıktan SONRA ödeme işlemini BAŞLATAN metot
		[HttpGet]
		public async Task<IActionResult> InitiatePurchase(string sessionIds)
		{
			// Bu metot, sizin eski ApplyOrPurchase metodunuzdaki "SATIN ALMA" mantığını içerir.
			var userId = _userManager.GetUserId(User);
			var ids = sessionIds.Split(',').Select(int.Parse).ToList();
			var firstSession = await _context.Sessions.Include(s => s.Service).FirstOrDefaultAsync(s => s.SessionId == ids.First());

			// Fiyatı hesapla
			var user = await _userManager.FindByIdAsync(userId);
			decimal price = 0m;
			switch (user.UserType)
			{
				case UserType.Öğrenci: price = firstSession.Service.StudentPrice; break;
				case UserType.Personel: price = firstSession.Service.StaffPrice; break;
				case UserType.Üye: price = firstSession.Service.MemberPrice; break;
			}

			// Mevcut başvuruları "Ödemede" olarak güncelle veya yenisini oluştur
			var existingRequests = await _context.ApplicationRequests.Where(ar => ar.UserId == userId && ids.Contains(ar.SessionId)).ToListAsync();
			if (existingRequests.Any())
			{
				foreach (var req in existingRequests) { req.EnrollmentStatus = EnrollmentStatus.Ödemede; }
			}
			else
			{
				foreach (var sessionId in ids)
				{
					_context.ApplicationRequests.Add(new ApplicationRequest { UserId = userId, SessionId = sessionId, EnrollmentStatus = EnrollmentStatus.Ödemede, RequestDate = DateTime.Now });
				}
			}
			await _context.SaveChangesAsync();

			// Ödeme sayfasına yönlendir
			return RedirectToAction("PayCreate", new { sessionIds, price, serviceId = firstSession.ServiceId });
		}

		[HttpGet]
		public async Task<IActionResult> Announcement()
		{
			var activeAnnouncements = await _context.Announcements
				.Where(a => a.EndDate.Date >= DateTime.Today)
				.OrderByDescending(a => a.AddDate)
				.ToListAsync();
			return View("Announcement", activeAnnouncements);
		}
	}
}
