using Microsoft.EntityFrameworkCore;
using SaucyMVC.Data;
using SaucyMVC.Services;

var builder = WebApplication.CreateBuilder(args);
// Console and debug logging work without requiring access to the Windows Event Log.
// This prevents an ordinary application warning from terminating a request.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
var cs = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

// Local MySQL 8 commonly uses caching_sha2_password without a configured TLS
// certificate. Keep this development convenience limited to localhost.
if (cs.Contains("Server=localhost", StringComparison.OrdinalIgnoreCase)) {
    if (!cs.Contains("SslMode=", StringComparison.OrdinalIgnoreCase))
        cs = cs.TrimEnd(';') + ";SslMode=None;";
    if (!cs.Contains("AllowPublicKeyRetrieval=", StringComparison.OrdinalIgnoreCase))
        cs = cs.TrimEnd(';') + ";AllowPublicKeyRetrieval=True;";
}

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SaucyDbContext>(o =>
    o.UseMySql(cs, ServerVersion.AutoDetect(cs)));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => {
    o.IdleTimeout = TimeSpan.FromHours(4);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderWorkflowService>();
builder.Services.Configure<ReservationAutomationOptions>(
    builder.Configuration.GetSection(ReservationAutomationOptions.SectionName));
builder.Services.AddScoped<ReservationAutomationService>();
builder.Services.AddHostedService<ReservationAutomationWorker>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope()) {
    try {
        var db = scope.ServiceProvider.GetRequiredService<SaucyDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSchemaUpdater.EnsureCurrentSchemaAsync(db);
        await DatabaseSeeder.SeedAsync(db);
    } catch (Exception ex) {
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        log.LogError(ex, "Database initialization failed. Create saucy_db in MySQL Workbench and verify appsettings.json.");
    }
}
app.Run();
