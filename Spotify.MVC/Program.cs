using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.Interface;
using Spotify.MVC.Services;
using static System.Net.WebRequestMethods;

CRUD<Cancion>.EndPoint = "https://localhost:7028/api/Canciones";
CRUD<Usuario>.EndPoint = "https://localhost:7028/api/Usuarios";
CRUD<Plan>.EndPoint = "https://localhost:7028/api/Planes";
CRUD<Playlist>.EndPoint = "https://localhost:7028/api/Playlists";

var builder = WebApplication.CreateBuilder(args);

// Contexto de EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDbContext")));

// Cache distribuido y sesión
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;                       // Solo accesible desde HTTP
    options.Cookie.IsEssential = true;                       // Siempre se envía
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Servicios de tu aplicación
builder.Services.AddScoped<IAutorizarService, AutorizarService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// MVC y autenticación por cookies
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Login/Index";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

// Para acceder al HttpContext en servicios
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Seed del admin
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!ctx.Usuarios.Any(u => u.TipoUsuario == "admin"))
    {
        ctx.Usuarios.Add(new Usuario
        {
            Email = "dylant0909@gmail.com",
            Nombre = "Dylan",
            Contraseña = BCrypt.Net.BCrypt.HashPassword("123"),
            TipoUsuario = "admin",
            FechaRegistro = DateTime.UtcNow
        });
        ctx.SaveChanges();
    }
}

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Orden correcto: sesión primero, luego auth
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
