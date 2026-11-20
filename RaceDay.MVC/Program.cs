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

// 5. Register HttpClient to talk to the API
builder.Services.AddHttpClient("RaceDayAPI", client =>
{
    // API base URL comes from configuration (Api:BaseUrl); defaults to the local dev API.
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7082/");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        // UseCookies = true ensures that when the API sends a Session Cookie, 
        // the MVC app stores it and sends it back on the next request automatically.
        UseCookies = true,
        CookieContainer = new System.Net.CookieContainer(),
        // Ignore SSL certificate errors for localhost development
        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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