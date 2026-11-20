using RaceDay.MVC.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add MVC services
builder.Services.AddControllersWithViews();

// 2. Add HttpContextAccessor (Required to access Session in other classes later)
builder.Services.AddHttpContextAccessor();

// 3. Register ApiService
builder.Services.AddScoped<ApiService>();

// 4. Setup Session Management for MVC (to keep the user logged in)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".RaceDay.MVC.Session";
});

builder.Services.AddTransient<ApiSessionHandler>();

// 5. Register HttpClient to talk to the API
builder.Services.AddHttpClient("RaceDayAPI", client =>
{
    // FIXED: Reads Docker variable (ApiSettings:BaseUrl) OR local appsettings (Api:BaseUrl)
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"]
                  ?? builder.Configuration["Api:BaseUrl"]
                  ?? "https://localhost:7082/";
    client.BaseAddress = new Uri(baseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Cookies are handled per user by ApiSessionHandler, not by a shared container
    UseCookies = false,
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
})
.AddHttpMessageHandler<ApiSessionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// FIXED: Commented out for Docker so it doesn't get stuck in an HTTP redirect loop
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// 6. Enable Session (Must be placed before Authorization)
app.UseSession();
app.UseAuthorization();

// 7. Default Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();