using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spotify.APIConsumer;
using Spotify.Modelos;
using System.Security.Claims;

namespace Spotify.MVC.Controllers
{
    public class PlanesController : Controller
    {
        private readonly AppDbContext _context;

        public PlanesController(AppDbContext context)
        {
            _context = context;
        }

        // Ver los planes disponibles
        public IActionResult Index()
        {
            var planes = _context.Planes.ToList();
            return View(planes); // Mostramos todos los planes disponibles
        }

        // Comprar un plan
        [HttpPost]
        public async Task<IActionResult> ComprarPlan(int planId)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var plan = await _context.Planes.FindAsync(planId);

            if (plan == null)
            {
                // Si no existe el plan
                return NotFound();
            }

            if (usuario.Plan != null && usuario.Plan.Id == planId)
            {
                // El usuario ya tiene este plan
                return RedirectToAction("Dashboard", "Home");
            }

            // Aquí puedes verificar si el usuario tiene suficiente saldo
            if (usuario.Saldo < plan.PrecioMensual)
            {
                // Si no tiene saldo suficiente
                ViewBag.ErrorMessage = "No tienes suficiente saldo para comprar este plan.";
                return RedirectToAction("Index", "Planes");
            }

            // Descontar el saldo
            usuario.Saldo -= plan.PrecioMensual;
            usuario.Plan = plan;  // Asignar el nuevo plan

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard", "Home");
        }


        [HttpGet]
        public IActionResult RecargarSaldo()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = _context.Usuarios
                                  .FirstOrDefault(u => u.Id == usuarioId);
            if (usuario == null)
                return NotFound();

            return View(usuario); // Retorna la vista de recarga, independientemente del plan
        }

        // POST: /Planes/RecargarSaldo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecargarSaldo(double monto)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await _context.Usuarios
                                        .FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null) return NotFound();

            usuario.Saldo = (usuario.Saldo ?? 0) + monto; // Sumar el monto al saldo existente
            _context.Update(usuario);
            await _context.SaveChangesAsync();

            ViewBag.SuccessMessage = $"Has recargado ${monto} con éxito.";
            return RedirectToAction("Dashboard", "Home"); // Redirigir al dashboard
        }

        [HttpPost]
    public async Task<IActionResult> CambiarPlan(int nuevoPlanId)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Obtener el id del usuario logueado
        var usuario = await _context.Usuarios.FindAsync(usuarioId); // Obtener el usuario logueado

        var nuevoPlan = await _context.Planes.FindAsync(nuevoPlanId); // Obtener el nuevo plan seleccionado

        if (usuario == null || nuevoPlan == null)
        {
            return NotFound();
        }

        // Verificar si el usuario ya tiene un plan activo
        if (usuario.Plan != null)
        {
            ViewBag.ErrorMessage = "Ya tienes un plan activo. Debes esperar a que termine la suscripción para cambiar de plan.";
            return RedirectToAction("Dashboard", "Home");
        }

        // Verificar si el usuario tiene suficiente saldo para comprar el nuevo plan
        if (usuario.Saldo >= nuevoPlan.PrecioMensual)
        {
            usuario.Saldo -= nuevoPlan.PrecioMensual; // Descontar el saldo
            usuario.Plan = nuevoPlan; // Asignar el nuevo plan

            _context.Update(usuario); // Actualizar el usuario con el nuevo plan
            await _context.SaveChangesAsync(); // Guardar los cambios

            ViewBag.SuccessMessage = $"¡Plan {nuevoPlan.Nombre} activado con éxito!";
            return RedirectToAction("Dashboard", "Home");
        }

        // Si no tiene saldo suficiente, mostrar un mensaje de error
        ViewBag.ErrorMessage = "No tienes suficiente saldo para cambiar de plan.";
        return RedirectToAction("Index", "Planes");
    }


    }

}