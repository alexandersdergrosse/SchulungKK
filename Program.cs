using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Fonts;
using SchulungKK.Data;
using SchulungKK.Services;


var builder = WebApplication.CreateBuilder(args);

const long MaximaleVideoDateigroesse = 1_073_741_824; // 1 GB

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaximaleVideoDateigroesse;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaximaleVideoDateigroesse;
});

if (OperatingSystem.IsWindows())
{
    GlobalFontSettings.UseWindowsFontsUnderWindows = true;
}

builder.Services.AddRazorPages();

builder.Services.AddScoped<ZertifikatService>();

builder.Services.AddScoped<WordQuizImportService>();

builder.Services.AddDbContext<SchulungenDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
