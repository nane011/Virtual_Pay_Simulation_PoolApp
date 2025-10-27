using Virtual_Pay_Simulation.Data;
using Microsoft.EntityFrameworkCore;
using System;
using Virtual_Pay_Simulation.Controllers;
using Virtual_Pay_Simulation.Interfaces;
using MailKit.Net.Smtp;
using MailKit;
using Virtual_Pay_Simulation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<IEmailService, EmailService>(); // Interface ? Class baðlantýsý


// Identity servislerini ekle
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.Password.RequireDigit = false;
	options.Password.RequiredLength = 6;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = false;
	options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();


builder.Services.AddSession();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

var app=builder.Build();
// Rolleri oluþtur
using (var scope = app.Services.CreateScope())
{
	var serviceProvider = scope.ServiceProvider;
	await CreateRoles(serviceProvider);
}

async Task CreateRoles(IServiceProvider serviceProvider)
{
	var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

	string[] roleNames = { "Admin", "User" };

	foreach (var roleName in roleNames)
	{
		var roleExist = await roleManager.RoleExistsAsync(roleName);
		if (!roleExist)
		{
			await roleManager.CreateAsync(new IdentityRole(roleName));
		}
	}
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Shared/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseWebSockets();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Public}/{action=FirstPage}/{id?}"); 

app.Run();

public static class ViewRenderer
{
	public static async Task<string> RenderViewToStringAsync<TModel>(this Controller controller, string viewName, TModel model)
	{
		controller.ViewData.Model = model;
		using (var sw = new StringWriter())
		{
			var viewEngine = controller.HttpContext.RequestServices
				.GetRequiredService<ICompositeViewEngine>();

			var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, false);

			if (!viewResult.Success)
			{
				throw new ArgumentNullException($"{viewName} view bulunamadý");
			}

			var viewContext = new ViewContext(
				controller.ControllerContext,
				viewResult.View,
				controller.ViewData,
				controller.TempData,
				sw,
				new HtmlHelperOptions()
			);

			await viewResult.View.RenderAsync(viewContext);
			return sw.ToString();
		}
	}
}