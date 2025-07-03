using Spotify.APIConsumer;
using Spotify.Modelos;
using Spotify.MVC.Interface;
using Spotify.MVC.Services;
using static System.Net.WebRequestMethods;

CRUD<Cancion>.EndPoint = "https://localhost:7028/api/Canciones";
CRUD<Usuario>.EndPoint = "https://localhost:7028/api/Usuarios"; 
CRUD<Plan>.EndPoint = "https://localhost:7028/api/Planes"; 
//CRUD<Suscripcion>.EndPoint = "https://localhost:7028/api/Suscripciones"; 


var builder = WebApplication.CreateBuilder(args);

//Registrar Servicios

builder.Services.AddScoped<IAutorizarService, AutorizarService>();
builder.Services.AddScoped<IEmailService, EmailService>();


// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication("Cookies") 
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Login/Index"; 


    });
builder.Services.AddHttpContextAccessor(); 
var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication(); 

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


