using Microsoft.AspNetCore.Mvc;
using Virtual_Pay_Simulation.Data;
using Virtual_Pay_Simulation.Models;
using Virtual_Pay_Simulation.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Virtual_Pay_Simulation.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace Virtual_Pay_Simulation.Controllers
{
	//[Authorize(Roles="Admin")]//sadece admin erişebilir.
	public class AdminController : Controller
	{
		private readonly AppDbContext _context; //bu context bağlantısı  
		private readonly IEmailService _emailService; // EmailService bağı  
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly ILogger<AdminController> _logger; // Loglama için ILogger bağı

		public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AdminController> logger) // Veritabanı bağlantısını alır
		{
			_context = context;
			_emailService = new EmailService(); // EmailService nesnesi
			_userManager = userManager;
			_signInManager = signInManager;
			_logger = logger;
		}

		[HttpGet]
		public async Task <IActionResult> AdminPan()
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
		public async Task<IActionResult> Profile()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return Challenge(); // Kullanıcı bulunamazsa giriş sayfasına yönlendir.
			}

			var viewModel = new ProfileViewModel
			{
				FullName = $"{user.Name} {user.Surname}",
				TCKimlikNo = user.UserName,
				Email = user.Email,
				PhoneNumber = user.PhoneNumber,
				UserType = "Admin", // Admin olduğu için doğrudan Admin yazabiliriz.
				Gender = user.Gender.ToString(),
				WristBand = user.WristBand,
				PenaltyCount = await _context.UserPenalties.CountAsync(p => p.UserId == user.Id)
			};

			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Register(RegisterViewModel model)
		{
			if (model.UserType == null)
			{
				ModelState.AddModelError("UserType", "Kullanıcı türü seçmelisiniz!");
			}
			
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
					UserType=model.UserType,
					Surname = model.Surname,
					PhoneNumber = model.Phone,
					WristBand = model.WristBand
				};

				// Kullanıcıyı oluştur  ve rastgele oluşturulmuş şifreyi kullanıcıya ata.
				var result = await _userManager.CreateAsync(user, randomPassword);

				if (result.Succeeded)
				{
					
					// Rol ataması yap
					string roleName = model.RoleId == 1 ? "Admin" : "User";
					await _userManager.AddToRoleAsync(user, roleName);

					//Şifreyi mail ile kullanıcıya gönder.
					await _emailService.SendPasswordEmail(user.Email, randomPassword);

					TempData["RegisterSuccess"] = "Kayıt oluşturuldu. Şifre gönderiliyor.";
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

		[HttpGet]
		public IActionResult CreateService()
		{
			var model = new ServiceCreateViewModel
			{
				StartTime = DateTime.Today,
				EndTime = DateTime.Today.AddMonths(1),
			};

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> CreateService(ServiceCreateViewModel model)
		{
			if (ModelState.IsValid)
			{
				var service = new Service
				{
					Title = model.Title,
					Description = model.Description,
					StudentPrice = model.StudentPrice,
					StaffPrice = model.StaffPrice,
					MemberPrice = model.MemberPrice,
					StartTime = model.StartTime,
					EndTime = model.EndTime,
					PayDueDate = model.PayDueDate
				};

				_context.Services.Add(service);
				await _context.SaveChangesAsync();

				// Kaydettikten sonra ID oluşmuş olur
				HttpContext.Session.SetString("CurrentServiceTitle", service.Title);
				HttpContext.Session.SetInt32("CurrentServiceId", service.ServiceId);

				TempData["Success"] = "Hizmet başarıyla oluşturuldu!";
				return RedirectToAction("SeanceCreate"); // Seans oluşturma sayfasına yönlendirme
			}

			return View(model);
		}


		[HttpGet]
		public IActionResult DayIndex()
		{
			var templates = _context.ServiceDayTemplates
				.Include(t => t.Days)
				.ToList();

			return View(templates);
		}

		[HttpPost]
		public IActionResult DayIndex(Gender gender, List<DayOfWeek> selectedDays)
		{
			if (!selectedDays.Any())
				return RedirectToAction("DayIndex");

			// Template adı otomatik verilsin: "Kadın Kalıbı 1" gibi
			var count = _context.ServiceDayTemplates.Count(t => t.Gender == gender);
			var templateName = $"{gender} Kalıbı {count + 1}";

			var template = new ServiceDayTemplate
			{
				TemplateName = templateName,
				Gender = gender,
				Days = selectedDays.Select(d => new ServiceDay { Day = d }).ToList()
			};

			_context.ServiceDayTemplates.Add(template);
			_context.SaveChanges();

			return RedirectToAction("DayIndex");
		}

		[HttpPost]
		public async Task<IActionResult> DeleteTemplate(int id)
		{
			var template = await _context.ServiceDayTemplates
				.Include(t => t.Days)
				.FirstOrDefaultAsync(t => t.TemplateId == id);

			if (template == null)
			{
				return Json(new { success = false, message = "Kalıp bulunamadı." });
			}

			// YENİ KRİTİK KONTROL: Bu kalıbın kullanılıp kullanılmadığını kontrol et.
			// template.Days (ServiceDay) içindeki herhangi bir günün bir seansa (Session) bağlı olup olmadığını kontrol et.
			var isUsedInService = await _context.ServiceDays
				.Where(d => d.TemplateId == id)
				.AnyAsync(d => d.Sessions.Any()); // ServiceDay'lerin herhangi bir seansı var mı?

			if (isUsedInService)
			{
				// KRİTİK: Eğer kalıp herhangi bir seans oluşturmak için kullanıldıysa, silmeyi tamamen engelle.
				// Bu, geçmiş kayıtlarınızın kaybolmasını kesin olarak önler.
				return Json(new
				{
					success = false,
					message = "BU KALIP AKTİF VEYA GEÇMİŞ HİZMETLERDE KULLANILMIŞTIR! Veri bütünlüğünü korumak adına silinemez. Silme işlemi, tüm ilişkili seans ve müşteri kayıtlarını kalıcı olarak silecektir."
				});
			}

			// Kalıp HİÇBİR SEANSTA kullanılmamışsa, silme işlemine devam edebiliriz.
			if (template.Days.Any())
			{
				_context.ServiceDays.RemoveRange(template.Days);
			}

			_context.ServiceDayTemplates.Remove(template);
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Kalıp başarıyla silindi (Geçmiş kayıtlarda kullanılmamıştır)." });
		}

		[HttpGet]
		public IActionResult SeanceCreate()
		{
			return View();
		}

		[HttpGet]
		public IActionResult FullSeanceCreate(int serviceId)
		{
			var service = _context.Services.FirstOrDefault(s => s.ServiceId == serviceId);
			if (service == null) return NotFound();

			var model = new SeanceViewModel
			{
				ServiceId = serviceId,
				ServiceTitle = service.Title,
				Templates = _context.ServiceDayTemplates.ToList(),
				Seances = new List<SeanceInput> { new SeanceInput() } // Formda en az bir saat girişi alanı olsun
			};

			return View(model);
		}

		[HttpPost]
        public IActionResult FullSeanceCreate(SeanceViewModel model)
        {
            if (!ModelState.IsValid || !model.SelectedTemplateIds.Any() || !model.Seances.Any())
            {
                ModelState.AddModelError("", "Lütfen en az bir gün kalıbı ve bir seans saati giriniz.");
                model.Templates = _context.ServiceDayTemplates.ToList();
                return View(model);
            }

            try
            {
                var selectedTemplates = _context.ServiceDayTemplates
                    .Where(t => model.SelectedTemplateIds.Contains(t.TemplateId))
                    .Include(t => t.Days)
                    .ToList();

                if (!selectedTemplates.Any())
                {
                    ModelState.AddModelError("", "Seçilen gün kalıpları bulunamadı.");
                    model.Templates = _context.ServiceDayTemplates.ToList();
                    return View(model);
                }

                // Hatalı olan tarih döngüsü kaldırıldı.
                // Artık sadece seçilen kalıplar, günler ve saatler üzerinden ilerliyoruz.
                foreach (var template in selectedTemplates)
                {
                    foreach (var serviceDay in template.Days)
                    {
                        foreach (var seanceInput in model.Seances)
                        {
                            // Her gün ve saat kombinasyonu için SADECE BİR TANE seans kaydı oluştur.
                            var session = new Session
                            {
                                ServiceId = model.ServiceId,
                                ServiceDayId = serviceDay.ServiceDayId,
                                StartTime = seanceInput.StartTime,
                                EndTime = seanceInput.EndTime,
                                MinCapacity = seanceInput.MinCapacity,
                                BorderCapacity = seanceInput.BorderCapacity,
                                MaxCapacity = seanceInput.MaxCapacity,
                                CurrentCapacity = 0,
                                IsActive = true
                            };
                            _context.Sessions.Add(session);
                        }
                    }
                }

                _context.SaveChanges();
                TempData["Success"] = "Seanslar başarıyla oluşturuldu.";
                return RedirectToAction("CurrentServices", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seans oluşturma sırasında hata oluştu.");
                ModelState.AddModelError("", "Seanslar kaydedilirken beklenmedik bir hata oluştu.");
                model.Templates = _context.ServiceDayTemplates.ToList();
                return View(model);
            }
        }


		[HttpGet]
		public IActionResult SeperateSeance(int serviceId)
		{
			var service = _context.Services.FirstOrDefault(s => s.ServiceId == serviceId);
			if (service == null) return NotFound();

			var templates = _context.ServiceDayTemplates
				.Include(t => t.Days)
				.ToList();

			var model = new SeanceViewModel
			{
				ServiceId = serviceId,
				Templates = templates
			};

			return View(model);
		}

		[HttpPost]
		public IActionResult SaveSingleSeance(int serviceId, int templateId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime,
												int minCapacity, int borderCapacity, int maxCapacity)
		{
			var serviceDay = _context.ServiceDays
				.FirstOrDefault(sd => sd.TemplateId == templateId && sd.Day == dayOfWeek);

			if (serviceDay == null)
			{
				TempData["Error"] = "Gün bulunamadı.";
				return RedirectToAction("SeperateSeance", new { serviceId });
			}

			var session = new Session
			{
				ServiceId = serviceId,
				ServiceDayId = serviceDay.ServiceDayId,
				StartTime = startTime,
				EndTime = endTime,
				MinCapacity = minCapacity,
				BorderCapacity = borderCapacity,
				MaxCapacity = maxCapacity,
				CurrentCapacity = 0,
				IsActive = true
			};

			_context.Sessions.Add(session);
			_context.SaveChanges();

			TempData["Success"] = "Seans başarıyla eklendi.";
			return RedirectToAction("SeperateSeance", new { serviceId });
		}

		[HttpGet]
		public IActionResult CurrentServices(string status = "active")
		{
			IQueryable<Service> services = _context.Services;
			// HATA DÜZELTMESİ: Status bilgisini View'a gönderiyoruz.
			ViewBag.Status = status;

			switch (status)
			{
				case "active":
					services = services.Where(s => s.IsActive && s.EndTime >= DateTime.Today);
					break;
				case "inactive":
					services = services.Where(s => !s.IsActive);
					break;
				case "past":
					services = services.Where(s => s.EndTime < DateTime.Today);
					break;
			}
			return View(services.OrderByDescending(s => s.StartTime).ToList());
		}

		[HttpGet]
		public async Task<IActionResult> ServiceInfo(int id)
		{
			var service = await _context.Services.FindAsync(id);
			if (service == null) return NotFound();

			var culture = new CultureInfo("tr-TR");

			// --- HATA GİDERİLMİŞ KOD ---
			// 1. AŞAMA: Veritabanından sadece ham verileri, gruplanmış bir şekilde çekiyoruz.
			// Burada C#'a özel hiçbir metin formatlama işlemi yapmıyoruz.
			var groupedData = await _context.Sessions
				.Where(s => s.ServiceId == id && s.ServiceDay != null && s.ServiceDay.ServiceDayTemplate != null)
				.GroupBy(s => new {
					s.ServiceDay.TemplateId,
					s.StartTime,
					s.EndTime,
					s.MaxCapacity,
					s.ServiceDay.ServiceDayTemplate.TemplateName,
					s.ServiceDay.ServiceDayTemplate.Gender
				})
				.Select(g => new {
					GroupKey = g.Key,
					DaysOfWeek = g.Select(s => s.ServiceDay.Day).Distinct(),
					Requests = g.SelectMany(s => s.ApplicationRequests)
				})
				.ToListAsync();

			// 2. AŞAMA: Veriler artık uygulamada. Şimdi C# kullanarak istediğimiz formata çeviriyoruz.
			// Entity Framework burada devre dışı olduğu için GetDayName gibi C# fonksiyonları sorunsuzca çalışır.
			var sessionGroups = groupedData.Select(g => new AdminSessionGroupViewModel
			{
				GroupIdentifier = g.GroupKey.TemplateId + "_" + g.GroupKey.StartTime.ToString(@"hhmm"),
				TemplateName = g.GroupKey.TemplateName,
				Gender = g.GroupKey.Gender,
				StartTime = g.GroupKey.StartTime,
				EndTime = g.GroupKey.EndTime,
				MaxCapacity = g.GroupKey.MaxCapacity,
				Days = string.Join(", ", g.DaysOfWeek.ToList().Select(day => culture.DateTimeFormat.GetDayName(day))),
				ApplicantCount = g.Requests.Count(ar => !ar.PaymentComplete),
				ParticipantCount = g.Requests.Count(ar => ar.PaymentComplete)
			}).ToList();

			var viewModel = new AdminServiceInfoViewModel
			{
				Service = service,
				SessionGroups = sessionGroups
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> SessionDetail(int serviceId, string groupIdentifier)
		{
			var parts = groupIdentifier.Split('_');
			if (parts.Length != 2 || !int.TryParse(parts[0], out var templateId) || !TimeSpan.TryParseExact(parts[1], "hhmm", CultureInfo.InvariantCulture, out var startTime))
			{
				return BadRequest("Grup tanımlayıcı formatı hatalı.");
			}

			var service = await _context.Services.FindAsync(serviceId);
			if (service == null) return NotFound();

			var sessionsInGroup = await _context.Sessions
				.Where(s => s.ServiceId == serviceId && s.ServiceDay.TemplateId == templateId && s.StartTime == startTime)
				.Include(s => s.ServiceDay)
				.ToListAsync();

			if (!sessionsInGroup.Any()) return NotFound("Seans grubu bulunamadı.");

			var sessionDaysOfWeek = sessionsInGroup.Select(s => s.ServiceDay.Day).ToList();
			var sessionDates = new List<DateTime>();
			for (var date = service.StartTime.Date; date <= service.EndTime.Date; date = date.AddDays(1))
			{
				if (sessionDaysOfWeek.Contains(date.DayOfWeek))
				{
					sessionDates.Add(date);
				}
			}

			var firstSession = sessionsInGroup.First();
			var culture = new CultureInfo("tr-TR");
			var days = string.Join(", ", sessionsInGroup.Select(s => culture.DateTimeFormat.GetDayName(s.ServiceDay.Day)).Distinct());

			var viewModel = new AdminSessionDetailViewModel
			{
				ServiceId = serviceId,
				ServiceTitle = service.Title,
				GroupIdentifier = groupIdentifier,
				SessionInfo = $"{days} | {firstSession.StartTime:hh\\:mm} - {firstSession.EndTime:hh\\:mm}",
				SessionDates = sessionDates
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> LoadParticipantsPartial(int serviceId, string groupIdentifier)
		{
			try
			{
				var parts = groupIdentifier.Split('_');
				var templateId = int.Parse(parts[0]);
				var startTime = TimeSpan.ParseExact(parts[1], "hhmm", CultureInfo.InvariantCulture);

				// İki metodun da aynı şekilde çalışmasını garantilemek için .Include() komutu eklendi.
				var sessionIdsInGroup = await _context.Sessions
					.Include(s => s.ServiceDay)
					.Where(s => s.ServiceId == serviceId && s.ServiceDay.TemplateId == templateId && s.StartTime == startTime)
					.Select(s => s.SessionId)
					.ToListAsync();

				if (!sessionIdsInGroup.Any())
				{
					return PartialView("_ParticipantsPartial", new AdminParticipantsViewModel { Participants = new List<ApplicationUser>() });
				}

				var paidUserIds = await _context.ApplicationRequests
					.Where(ar => sessionIdsInGroup.Contains(ar.SessionId) && ar.PaymentComplete)
					.Select(ar => ar.UserId)
					.Distinct()
					.ToListAsync();

				var paidUsers = await _userManager.Users
					.Where(u => paidUserIds.Contains(u.Id))
					.OrderBy(u => u.Name)
					.ToListAsync();

				var viewModel = new AdminParticipantsViewModel
				{
					Participants = paidUsers,
					Applicants = new List<ApplicationUser>()
				};

				return PartialView("_ParticipantsPartial", viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Katılımcı listesi yüklenirken KRİTİK HATA oluştu. GroupIdentifier: {groupIdentifier}", groupIdentifier);
				return PartialView("_ParticipantsPartial", new AdminParticipantsViewModel { Participants = new List<ApplicationUser>() });
			}
		}

		// --- KESİN ÇÖZÜM: Günlük Yoklama Listesini Yükler ---
		[HttpGet]
		public async Task<IActionResult> LoadDailyAttendance(int serviceId, string groupIdentifier, DateTime date)
		{
			try
			{
				var parts = groupIdentifier.Split('_');
				if (parts.Length != 2 || !int.TryParse(parts[0], out var templateId) || !TimeSpan.TryParseExact(parts[1], "hhmm", CultureInfo.InvariantCulture, out var startTime))
				{
					return PartialView("_DailyAttendancePartial", new AdminDailyAttendanceViewModel { IsFutureDate = true, Date = date });
				}

				if (date.Date > DateTime.Today || (date.Date == DateTime.Today && startTime > DateTime.Now.TimeOfDay))
				{
					return PartialView("_DailyAttendancePartial", new AdminDailyAttendanceViewModel { IsFutureDate = true, Date = date });
				}

				// İki metodun da aynı şekilde çalışmasını garantilemek için .Include() komutu eklendi.
				var sessionIdsInGroup = await _context.Sessions
					.Include(s => s.ServiceDay)
					.Where(s => s.ServiceId == serviceId && s.ServiceDay.TemplateId == templateId && s.StartTime == startTime)
					.Select(s => s.SessionId)
					.ToListAsync();

				if (!sessionIdsInGroup.Any())
				{
					return PartialView("_DailyAttendancePartial", new AdminDailyAttendanceViewModel { Date = date });
				}

				var paidUserIds = await _context.ApplicationRequests
					.Where(ar => sessionIdsInGroup.Contains(ar.SessionId) && ar.PaymentComplete)
					.Select(ar => ar.UserId)
					.Distinct()
					.ToListAsync();

				var paidUsers = await _userManager.Users
					.Where(u => paidUserIds.Contains(u.Id))
					.ToListAsync();

				var attendanceRecordsForDate = await _context.Attendaces
					.Where(a => a.AttendanceDate.Date == date.Date && sessionIdsInGroup.Contains(a.SessionId))
					.ToDictionaryAsync(a => a.UserId, a => a.IsPresent);

				var viewModel = new AdminDailyAttendanceViewModel
				{
					Date = date,
					IsFutureDate = false,
					Attendees = paidUsers.Select(u => new UserAttendanceStatus
					{
						FullName = $"{u.Name} {u.Surname}",
						TCKimlikNo = u.UserName,
						IsPresent = attendanceRecordsForDate.ContainsKey(u.Id) && attendanceRecordsForDate[u.Id]
					}).ToList()
				};

				return PartialView("_DailyAttendancePartial", viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Günlük yoklama listesi yüklenirken hata oluştu. ServiceId: {serviceId}, Group: {groupIdentifier}, Date: {date}", serviceId, groupIdentifier, date);
				ModelState.AddModelError("", "Yoklama listesi yüklenirken bir hata oluştu.");
				return PartialView("_DailyAttendancePartial", new AdminDailyAttendanceViewModel { Date = date });
			}
		}

		

		[HttpPost]
		public IActionResult PublishService(int id)
		{
			var service = _context.Services.FirstOrDefault(s => s.ServiceId == id);
			if (service == null || service.IsActive)
				return NotFound();

			service.IsActive = true;
			_context.SaveChanges();

			TempData["Success"] = "Hizmet başarıyla yayınlandı.";
			return RedirectToAction("ServiceInfo", new { id });
		}

		[HttpPost]
		public IActionResult DeleteService(int id)
		{
			var service = _context.Services
				.Include(s => s.Sessions)
				.FirstOrDefault(s => s.ServiceId == id);

			if (service == null || service.IsActive)
				return NotFound();

			// Seanslar varsa önce onları silelim
			_context.Sessions.RemoveRange(service.Sessions);
			_context.Services.Remove(service);
			_context.SaveChanges();

			TempData["Success"] = "Hizmet silindi.";
			return RedirectToAction("CurrentServices");
		}

		[HttpGet]
		public async Task<IActionResult> PAccountCtrl()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> LoadUserAccounts()
		{
			var user = await _userManager.GetUsersInRoleAsync("User");

			var users=_userManager.Users.Where(u=>u.WBandState==true && u.IsBlocked==false).ToList();
			return PartialView("_UserAccountsPartial", users);
		}

		[HttpGet]
		public async Task<IActionResult> LoadUserDetails(string userId)
		{
			if(string.IsNullOrEmpty(userId))
			{
				return BadRequest();
			}

			var user = await _userManager.FindByIdAsync(userId);
			if(user==null)
			{
				return NotFound();
			}
			ViewBag.PenaltyCount = await _context.UserPenalties.CountAsync(p => p.UserId == userId);

			return PartialView("_UserDetailsPartial", user);
		}

		// --- FİLTRELEME YETENEĞİ EKLENMİŞ YENİ METOT ---
		[HttpGet]
		public async Task<IActionResult> LoadAttendanceHistory(string userId, DateTime? filterDate, bool showOnlyAttended = false)
		{
			if (string.IsNullOrEmpty(userId)) return BadRequest();

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null) return NotFound();

			var culture = new CultureInfo("tr-TR");

			// Sorguyu adım adım oluşturuyoruz.
			IQueryable<Attendance> query = _context.Attendaces.Where(a => a.UserId == userId);

			// 1. Filtre: Sadece katıldıklarını göster
			if (showOnlyAttended)
			{
				query = query.Where(a => a.IsPresent);
			}

			// 2. Filtre: Belirli bir tarih (Eğer tarih seçilmişse)
			if (filterDate.HasValue)
			{
				query = query.Where(a => a.AttendanceDate.Date == filterDate.Value.Date);
			}

			// Filtrelenmiş sorguyu çalıştır ve sonuçları al.
			var attendanceHistory = await query
				.Include(a => a.Session).ThenInclude(s => s.Service)
				.Include(a => a.Session).ThenInclude(s => s.ServiceDay)
				.OrderByDescending(a => a.AttendanceDate)
				.Select(a => new AttendanceRecordViewModel
				{
					AttendanceDate = a.AttendanceDate,
					ServiceName = a.Session.Service.Title,
					SessionDay = culture.DateTimeFormat.GetDayName(a.Session.ServiceDay.Day),
					SessionTime = $"{a.Session.StartTime:hh\\:mm} - {a.Session.EndTime:hh\\:mm}",
					IsPresent = a.IsPresent
				})
				.ToListAsync();

			var viewModel = new UserAttendanceHistoryViewModel
			{
				UserId = user.Id,
				UserFullName = $"{user.Name} {user.Surname}",
				HistoryRecords = attendanceHistory,
				// Filtre değerlerini View'a geri gönderiyoruz ki kullanıcı seçimlerini unutmasın.
				FilterDate = filterDate,
				FilterShowOnlyAttended = showOnlyAttended
			};

			return PartialView("_UserAttendanceHistoryPartial", viewModel);
		}

		[HttpPost]
		public async Task<IActionResult> ApproveUser(string userId)
		{
			var user = await _userManager.FindByIdAsync(userId);
			if (user != null)
			{
				// Modal için partial render
				return PartialView("_ActivationPartial", user);
			}
			return Json(new { success = false });
		}

		[HttpGet]
		public async Task<IActionResult> LoadActivationModal(string userId)
		{
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				return NotFound();
			}
			return PartialView("_ActivationPartial", user);
		}

		[HttpPost]
		public async Task<IActionResult> CompleteActivation(string userId, UserType userType, string wristBand)
		{
			try
			{
				var user = await _userManager.FindByIdAsync(userId);
				if (user == null)
				{
					return Json(new { success = false, message = "Kullanıcı bulunamadı" });
				}

				user.UserType = userType;
				user.WristBand = wristBand;
				user.WBandState = true;

				await _userManager.UpdateAsync(user);

				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		[HttpGet]
		public IActionResult LoadBlockedAccounts()
		{
			var users=_userManager.Users
				.Include(u=>u.UserPenalties)
				.ToList();
			return PartialView("_BlockedAccountsPartial", users);
		}

		[HttpPost]
		public async Task<IActionResult> UnblockUser(string userId)
		{
			if (string.IsNullOrEmpty(userId))
			{
				return Json(new { success = false, message = "Kullanıcı ID'si bulunamadı." });
			}

			// 1. Kullanıcıyı bul
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				return Json(new { success = false, message = "Kullanıcı bulunamadı." });
			}

			// 2. Kullanıcının tüm ceza puanlarını bul ve sil
			var penalties = _context.UserPenalties.Where(p => p.UserId == userId);
			if (penalties.Any())
			{
				_context.UserPenalties.RemoveRange(penalties);
				// Değişiklikleri kaydet
				await _context.SaveChangesAsync();
			}

			// 3. Kullanıcının IsBlocked bayrağını false yap
			user.IsBlocked = false;

			// 4. Kullanıcıyı güncelle
			var result = await _userManager.UpdateAsync(user);

			if (result.Succeeded)
			{
				// Başarılı olursa, JavaScript'e olumlu sinyal gönder
				return Json(new { success = true });
			}

			// Başarısız olursa, hatayı bildir
			return Json(new { success = false, message = "Kullanıcının blokesi kaldırılırken bir hata oluştu." });
		}

		[HttpPost]
		public async Task<IActionResult> ApplyPenaltiesForOverduePayments()
		{
			// Son ödeme tarihi geçmiş ve ödemesi yapılmamış tüm başvuruları bul.
			var overdueRequests = await _context.ApplicationRequests
				.Include(ar => ar.Session.Service)
				.Include(ar => ar.ApplicationUser)
				.Where(ar => !ar.PaymentComplete && ar.Session.Service.PayDueDate < DateTime.Today)
				.ToListAsync();

			if (!overdueRequests.Any())
			{
				return Json(new { success = true, message = "Son ödeme tarihi geçmiş ve ödenmemiş başvuru bulunamadı." });
			}

			var usersToPenalize = overdueRequests.GroupBy(ar => ar.ApplicationUser).ToList();
			int penalizedUserCount = 0;
			int blockedUserCount = 0;

			foreach (var userGroup in usersToPenalize)
			{
				var user = userGroup.Key;
				var serviceTitles = string.Join(", ", userGroup.Select(g => g.Session.Service.Title).Distinct());

				var newPenalty = new UserPenalty
				{
					UserId = user.Id,
					SessionId = userGroup.First().SessionId,
					Reason = $"Son ödeme tarihi geçen '{serviceTitles}' hizmet(ler)i için ödeme yapılmadı."
				};
				_context.UserPenalties.Add(newPenalty);
				penalizedUserCount++;

				var totalPenalties = await _context.UserPenalties.CountAsync(p => p.UserId == user.Id) + 1;

				if (totalPenalties >= 2 && !user.IsBlocked)
				{
					user.IsBlocked = true;
					blockedUserCount++;

					var subject = "Hesabınız Bloke Edilmiştir";
					var body = $"<p>Sayın {user.Name} {user.Surname},</p>" +
							   $"<p>Sistemimizde 2 veya daha fazla ceza puanına ulaştığınız için hesabınız bloke edilmiştir.</p>" +
							   "<p>Hesabınızın durumu hakkında bilgi almak için lütfen bizimle iletişime geçiniz.</p>";
					await _emailService.SendEmailAsync(user.Email, subject, body);
				}
			}

			_context.ApplicationRequests.RemoveRange(overdueRequests);
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = $"{penalizedUserCount} kullanıcıya ceza uygulandı. {blockedUserCount} kullanıcı bloke edildi." });
		}

		[HttpGet]
		public async Task<IActionResult> LoadPreRegistrations()
		{
			var inactiveusers =(await _userManager.GetUsersInRoleAsync("User"))
				.Where(u=>!u.WBandState && !u.IsBlocked)
				.ToList();
			return PartialView("_PreRegistrationsPartial", inactiveusers);
		}

		// --- YENİ EKLENECEK METOT ---
		[HttpPost]
		public async Task<IActionResult> DeleteUser(string userId)
		{
			if (string.IsNullOrEmpty(userId))
			{
				return Json(new { success = false, message = "Kullanıcı ID'si bulunamadı." });
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				return Json(new { success = false, message = "Silinecek kullanıcı bulunamadı." });
			}

			// DİKKAT: Bu işlem kullanıcıyı ve ilişkili tüm Identity verilerini
			// (roller, login bilgileri vb.) veritabanından kalıcı olarak siler.
			var result = await _userManager.DeleteAsync(user);

			if (result.Succeeded)
			{
				return Json(new { success = true });
			}

			// Eğer bir hata olursa, hatayı loglayıp admin'e genel bir mesaj gösterebiliriz.
			_logger.LogError("Kullanıcı silinirken hata oluştu: {errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
			return Json(new { success = false, message = "Kullanıcı silinirken bir hata oluştu. Lütfen tekrar deneyin." });
		}

		// --- YENİ SEKMELİ YAPI İÇİN METOTLAR ---

		// 1. Ana Sayfayı Yükleyen Metot
		[HttpGet]
		public IActionResult ContractManager()
		{
			// Bu metot artık sadece sekmeleri içeren ana çerçeveyi yükleyecek.
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ContractManager(Contract contractModel) // METOT ADI: ContractManagement
		{
			if (!ModelState.IsValid)
			{
				// Hata durumunda, formu tekrar göstermek için partial view'ı ve modeli hazırlamamız gerekir.
				// Bu en temiz yöntemdir. Ana sayfaya yönlendiriyoruz.
				TempData["Error"] = "Formda eksik veya hatalı bilgiler var. Lütfen kontrol ediniz.";
				return RedirectToAction("ContractManager");
			}

			var currentActiveContract = await _context.Contracts
				.OrderByDescending(c => c.Version)
				.FirstOrDefaultAsync(c => c.IsActive);

			if (currentActiveContract == null || currentActiveContract.Content != contractModel.Content)
			{
				await _context.Contracts.ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false));

				var newContract = new Contract
				{
					Title = contractModel.Title,
					Content = contractModel.Content,
					Version = (currentActiveContract?.Version ?? 0) + 1,
					IsActive = true,
					CreatedDate = DateTime.Now
				};
				_context.Contracts.Add(newContract);

				await _context.Users.ExecuteUpdateAsync(s => s.SetProperty(u => u.AcceptedContractVersion, (int?)null));
				TempData["Success"] = $"Sözleşme başarıyla güncellendi. Yeni versiyon ({newContract.Version}) aktif edildi ve tüm kullanıcı onayları sıfırlandı.";
			}
			else
			{
				currentActiveContract.Title = contractModel.Title;
				TempData["Success"] = "Sözleşme başlığı güncellendi. İçerik değişmediği için yeni versiyon oluşturulmadı.";
			}

			await _context.SaveChangesAsync();
			// İşlem bittiğinde, yeni sekmeli ana sayfamız olan "ContractManager"a yönlendiriyoruz.
			return RedirectToAction("ContractManager");
		}

		// 2. Aktif Sözleşme Sekmesinin İçeriğini Yükleyen Metot
		[HttpGet]
		public IActionResult LoadActiveContractPartial() // async'e artık gerek yok
		{
			// Veritabanından veri çekmek yerine, her zaman boş bir form oluştur.
			var newContractModel = new Contract();

			return PartialView("_ActiveContractPartial", newContractModel);
		}

		// --- YENİ EKLENEN METOT ---
		// 4. Sadece Aktif Sözleşmeyi Görüntüleme Sekmesini Yükleyen Metot
		[HttpGet]
		public async Task<IActionResult> LoadActiveContractViewPartial()
		{
			var activeContract = await _context.Contracts
				.OrderByDescending(c => c.Version)
				.FirstOrDefaultAsync(c => c.IsActive);

			// Bu metot, birazdan oluşturacağımız _ActiveContractViewPartial.cshtml dosyasını döndürecek.
			return PartialView("_ActiveContractViewPartial", activeContract);
		}

		// 3. Geçmiş Sözleşmeler Sekmesinin İçeriğini Yükleyen Metot
		[HttpGet]
		public async Task<IActionResult> LoadPastContractsPartial()
		{
			// Sadece aktif olmayan, yani eski versiyonları tarihe göre sıralayarak getir.
			var pastContracts = await _context.Contracts
				.Where(c => !c.IsActive)
				.OrderByDescending(c => c.Version)
				.ToListAsync();

			// Bu metot, birazdan oluşturacağımız _PastContractsPartial.cshtml dosyasını döndürecek.
			return PartialView("_PastContractsPartial", pastContracts);
		}

		// --- YENİ DUYURU YÖNETİMİ METOTLARI ---

		// 1. Duyuru Yönetimi Ana Sayfasını Yükleyen Metot
		[HttpGet]
		public IActionResult AnnouncementManager()
		{
			// Bu metot sadece sekmeleri içeren ana çerçeveyi (AnnouncementManager.cshtml) yükleyecek.
			return View();
		}

		// 2. "Yeni Duyuru Ekle" Sekmesinin İçeriğini Yükleyen Metot
		[HttpGet]
		public IActionResult LoadNewAnnouncementPartial()
		{
			// Partial view'a boş bir model gönderiyoruz ki form oluşturulabilsin.
			var model = new Announcement
			{
				// Bitiş tarihi için varsayılan olarak 1 hafta sonrası seçili gelsin.
				EndDate = DateTime.Now.AddDays(7)
			};
			return PartialView("_NewAnnouncementPartial", model);
		}

		// 3. "Aktif Duyuruları" Yükleyen Metot
		[HttpGet]
		public async Task<IActionResult> LoadActiveAnnouncementsPartial()
		{
			var activeAnnouncements = await _context.Announcements
				.Where(a => a.EndDate.Date >= DateTime.Today)
				.OrderByDescending(a => a.AddDate)
				.ToListAsync();
			return PartialView("_ActiveAnnouncementsPartial", activeAnnouncements);
		}

		// 4. "Geçmiş Duyuruları" Yükleyen Metot
		[HttpGet]
		public async Task<IActionResult> LoadPastAnnouncementsPartial()
		{
			var pastAnnouncements = await _context.Announcements
				.Where(a => a.EndDate.Date < DateTime.Today)
				.OrderByDescending(a => a.AddDate)
				.ToListAsync();
			return PartialView("_PastAnnouncementsPartial", pastAnnouncements);
		}

		// 5. Yeni Duyuruyu Veritabanına Kaydeden Metot
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateAnnouncement(Announcement model)
		{
			if (ModelState.IsValid)
			{
				model.AddDate = DateTime.Now; // Ekleme tarihini o an olarak ayarla
				_context.Announcements.Add(model);
				await _context.SaveChangesAsync();

				TempData["Success"] = "Yeni duyuru başarıyla yayınlandı!";
				return RedirectToAction("AnnouncementManager");
			}

			// Model geçerli değilse, hata mesajlarıyla birlikte formu tekrar göstermesi için
			// TempData kullanarak modeli ve hataları bir sonraki request'e taşıyabiliriz.
			// Ancak en temiz yöntem, AJAX ile formu post edip client tarafında yönlendirme yapmaktır.
			// Şimdilik basit tutarak ana sayfaya yönlendiriyoruz.
			TempData["Error"] = "Formda eksik veya hatalı bilgiler var. Lütfen kontrol ediniz.";
			return RedirectToAction("AnnouncementManager");
		}

		// 6. Bir Duyuruyu Silen Metot
		[HttpPost]
		public async Task<IActionResult> DeleteAnnouncement(int id)
		{
			if (id <= 0)
			{
				return Json(new { success = false, message = "Geçersiz ID." });
			}

			var announcement = await _context.Announcements.FindAsync(id);
			if (announcement == null)
			{
				return Json(new { success = false, message = "Silinecek duyuru bulunamadı." });
			}

			_context.Announcements.Remove(announcement);
			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Duyuru başarıyla silindi." });
		}
	}
}