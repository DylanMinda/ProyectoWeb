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
        public async Task<IActionResult> Index()
        {
            var planes = await _context.Planes.ToListAsync();
            return View(planes);
        }

        public ActionResult Create()
        {
            return View();
        }

        // POST: HomeController1/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Plan plan)
        {
            try
            {
                // Usamos CRUD<Plan> para crear el nuevo plan a través de la API
                var nuevoPlan = CRUD<Plan>.Create(plan);

                // Redirigimos a la acción Index después de crear el plan
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }


        // Comprar un plan
        [HttpPost]
        public async Task<IActionResult> ComprarPlan(int planId)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Obtener el ID del usuario logueado
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);// Buscar el usuario por ID

            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var plan = await _context.Planes.FindAsync(planId);// Buscar el plan por ID
            if (plan == null)
            {
                return NotFound();
            }

            // VALIDACIÓN 1: Verificar si ya tiene este plan
            if (usuario.Plan != null && usuario.Plan.Id == planId)
            {
                TempData["ErrorMessage"] = "Ya tienes este plan activo.";
                return RedirectToAction("Index");
            }

            // VALIDACIÓN 2: Verificar saldo suficiente (SOLO para planes de pago)
            if (plan.PrecioMensual > 0 && (usuario.Saldo == null || usuario.Saldo < plan.PrecioMensual))// Verificar si el saldo es nulo o insuficiente
            {
                TempData["ErrorMessage"] = $"No tienes suficiente saldo para comprar este plan. Necesitas ${plan.PrecioMensual:F2} y tienes ${usuario.Saldo ?? 0:F2}.";// Mensaje de error con formato
                return RedirectToAction("Index");
            }

            // VALIDACIÓN 3: Prevenir downgrade de premium a gratuito
            if (usuario.Plan != null && usuario.Plan.PrecioMensual > 0 && plan.PrecioMensual == 0)// Verificar si el usuario tiene un plan premium y está intentando cambiar a un plan gratuito
            {
                TempData["ErrorMessage"] = "No puedes cambiar de un plan premium a un plan gratuito. Primero debes cancelar tu plan actual.";
                return RedirectToAction("Index");
            }

            // VALIDACIÓN 4: Si tiene un plan premium, solo puede upgradear a otro premium más caro
            if (usuario.Plan != null && usuario.Plan.PrecioMensual > 0 && plan.PrecioMensual > 0)// Verificar si el usuario tiene un plan premium y está intentando cambiar a otro plan premium
            {
                if (plan.PrecioMensual <= usuario.Plan.PrecioMensual)// Verificar si el nuevo plan es igual o más barato que el actual
                {
                    TempData["ErrorMessage"] = "Solo puedes upgradear a un plan superior. Para cambiar a un plan del mismo nivel o inferior, primero cancela tu plan actual.";
                    return RedirectToAction("Index");
                }
            }

            // PROCESAMIENTO: Descontar saldo SOLO si es un plan de pago
            if (plan.PrecioMensual > 0)//verificar si el plan tiene un costo mensual
            {
                usuario.Saldo -= plan.PrecioMensual;
            }

            // Asignar el nuevo plan
            usuario.Plan = plan;

            // Si es un plan grupal, generar código de invitación único
            if (plan.MaximoUsuarios > 1)// Verificar si el plan es grupal (más de 1 usuario)
            {
                usuario.CodigoInvitacion = GenerarCodigoInvitacion();// Generar un código de invitación único
            }
            else
            {
                // Limpiar código de invitación si no es plan grupal
                usuario.CodigoInvitacion = null;// Limpiar el código de invitación si no es un plan grupal
            }

            _context.Usuarios.Update(usuario);// Actualizar el usuario en la base de datos
            await _context.SaveChangesAsync();

            // Mensaje de éxito
            if (plan.MaximoUsuarios > 1)// Verificar si es un plan grupal
            {
                TempData["SuccessMessage"] = $"¡Plan {plan.Nombre} activado con éxito! Tu código de invitación es: {usuario.CodigoInvitacion}";// Mensaje de éxito con el código de invitación
            }
            else
            {
                if (plan.PrecioMensual == 0)// Verificar si el plan es gratuito
                {
                    TempData["SuccessMessage"] = $"¡Plan {plan.Nombre} activado con éxito!";
                }
                else
                {
                    TempData["SuccessMessage"] = $"¡Plan {plan.Nombre} activado con éxito! Se han descontado ${plan.PrecioMensual:F2} de tu saldo.";
                }
            }

            return RedirectToAction("Dashboard", "Home");
        }

        // Método para generar código de invitación único
        private string GenerarCodigoInvitacion()// Genera un código de invitación aleatorio de 8 caracteres
        {
            var random = new Random();// Crear una instancia de Random para generar números aleatorios
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";// Caracteres permitidos en el código de invitación
            return new string(Enumerable.Repeat(chars, 8)//repite los caracteres 8 veces
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // NUEVO: Unirse a plan grupal mediante código
        [HttpPost]
        public async Task<IActionResult> UnirseAPlan(string codigoInvitacion)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));// Obtener el ID del usuario logueado
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);// Buscar el usuario por ID

            if (usuario == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" });
            }

            // Verificar que no tenga un plan de pago activo
            if (usuario.Plan != null && usuario.Plan.PrecioMensual > 0)
            {
                return Json(new { success = false, message = "Ya tienes un plan de pago activo. Debes cancelarlo primero." });
            }

            // Buscar al administrador del plan con el código
            var administrador = await _context.Usuarios
                .Include(u => u.Plan)// Incluir el plan del usuario
                .FirstOrDefaultAsync(u => u.CodigoInvitacion == codigoInvitacion);// Buscar al usuario que tiene el código de invitación

            if (administrador == null || administrador.Plan == null || administrador.Plan.MaximoUsuarios <= 1)//verificar que el administrador tenga un plan grupal
            {
                return Json(new { success = false, message = "Código de invitación inválido o no corresponde a un plan grupal" });
            }

            // Contar usuarios actuales en el plan del administrador
            var usuariosEnPlan = await _context.Usuarios
                .Where(u => u.Plan != null && u.Plan.Id == administrador.Plan.Id && u.CodigoInvitacion == codigoInvitacion)
                .CountAsync();

            if (usuariosEnPlan >= administrador.Plan.MaximoUsuarios)// Verificar si el plan ha alcanzado su límite máximo de usuarios
            {
                return Json(new { success = false, message = $"El plan ha alcanzado su límite máximo de {administrador.Plan.MaximoUsuarios} usuarios" });// Verificar si el plan ha alcanzado su límite máximo de usuarios
            }

            // Verificar que no esté ya en este plan
            if (usuario.Plan != null && usuario.Plan.Id == administrador.Plan.Id && usuario.CodigoInvitacion == codigoInvitacion)
            {
                return Json(new { success = false, message = "Ya estás en este plan grupal" });
            }

            // Agregar usuario al plan (sin costo adicional)
            usuario.Plan = administrador.Plan;
            usuario.CodigoInvitacion = codigoInvitacion; // Importante: usar el mismo código que el administrador
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Te has unido exitosamente al plan {administrador.Plan.Nombre}"
            });
        }

        // Invitar usuario a plan grupal (MEJORADO - ahora solo comparte el código)
        [HttpGet]
        public async Task<IActionResult> CompartirCodigoInvitacion()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));// Obtener el ID del usuario logueado
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);// Buscar el usuario por ID

            if (usuario == null || usuario.Plan == null)// Verificar si el usuario o su plan son nulos
            {
                return Json(new { success = false, message = "No tienes un plan activo" });
            }

            // Verificar que es un plan grupal
            if (usuario.Plan.MaximoUsuarios <= 1)// Verificar si el plan es grupal (más de 1 usuario)
            {
                return Json(new { success = false, message = "Solo puedes compartir códigos en planes grupales" });
            }

            // Verificar que es el administrador (quien tiene el código original)
            if (string.IsNullOrEmpty(usuario.CodigoInvitacion))
            {
                return Json(new { success = false, message = "No tienes un código de invitación válido" });
            }

            // Verificar que es realmente el administrador (primer usuario con este código)
            var administrador = await _context.Usuarios
                .Where(u => u.Plan != null && u.Plan.Id == usuario.Plan.Id && u.CodigoInvitacion == usuario.CodigoInvitacion)// Buscar al administrador del plan
                .OrderBy(u => u.FechaRegistro)// Ordenar por fecha de registro para obtener al primer usuario
                .FirstOrDefaultAsync();// Obtener el primer usuario con este código

            if (administrador.Id != usuarioId)// Verificar que el usuario actual es el administrador
            {
                return Json(new { success = false, message = "Solo el administrador del plan puede compartir el código" });
            }
            // Si todo está bien, devolver el código de invitación para compartir
            return Json(new
            {
                success = true,
                codigoInvitacion = usuario.CodigoInvitacion,
                message = $"Comparte este código con las personas que quieras invitar: {usuario.CodigoInvitacion}"
            });
        }

        // Ver miembros del plan grupal (MEJORADO)
        [HttpGet]
        public async Task<IActionResult> MiembrosDelPlan()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));// Obtener el ID del usuario logueado
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);// Buscar el usuario por ID

            if (usuario == null || usuario.Plan == null)//verificar si el usuario o su plan son nulos
            {
                return RedirectToAction("Index");
            }

            // Verificar que es un plan grupal
            if (usuario.Plan.MaximoUsuarios <= 1)
            {
                TempData["ErrorMessage"] = "Esta función solo está disponible para planes grupales.";
                return RedirectToAction("Index");
            }

            // Obtener todos los miembros del plan con el mismo código de invitación
            var miembrosDelPlan = await _context.Usuarios
                .Where(u => u.Plan != null && u.Plan.Id == usuario.Plan.Id && u.CodigoInvitacion == usuario.CodigoInvitacion)// Filtrar usuarios que tienen el mismo plan y código de invitación
                .OrderBy(u => u.FechaRegistro)// Ordenar por fecha de registro para mostrar al administrador primero
                .ToListAsync();// Obtener la lista de miembros del plan

            // Determinar quién es el administrador (primer usuario)
            var administrador = miembrosDelPlan.FirstOrDefault();

            ViewBag.Plan = usuario.Plan;
            ViewBag.EsAdministrador = administrador?.Id == usuarioId;
            ViewBag.Administrador = administrador;
            ViewBag.EspaciosDisponibles = usuario.Plan.MaximoUsuarios - miembrosDelPlan.Count;
            ViewBag.CodigoInvitacion = usuario.CodigoInvitacion;

            return View(miembrosDelPlan);
        }

        // Expulsar miembro del plan (MEJORADO)
        [HttpPost]
        public async Task<IActionResult> ExpulsarMiembro(int miembroId)// Método para expulsar a un miembro del plan
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));// Obtener el ID del usuario logueado
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);// Buscar el usuario por ID

            if (usuario == null || usuario.Plan == null)
            {
                return Json(new { success = false, message = "No tienes un plan activo" });
            }

            // Verificar que es el administrador
            var administrador = await _context.Usuarios
                .Where(u => u.Plan != null && u.Plan.Id == usuario.Plan.Id && u.CodigoInvitacion == usuario.CodigoInvitacion)// Filtrar usuarios que tienen el mismo plan y código de invitación
                .OrderBy(u => u.FechaRegistro)// Ordenar por fecha de registro para obtener al administrador
                .FirstOrDefaultAsync();// Obtener el primer usuario con este código de invitación

            if (administrador.Id != usuarioId)
            {
                return Json(new { success = false, message = "Solo el administrador puede expulsar miembros" });
            }

            // No puede expulsarse a sí mismo
            if (miembroId == usuarioId)
            {
                return Json(new { success = false, message = "No puedes expulsarte a ti mismo del plan" });
            }

            var miembro = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == miembroId);
            if (miembro == null || miembro.Plan?.Id != usuario.Plan.Id || miembro.CodigoInvitacion != usuario.CodigoInvitacion)// Verificar que el miembro existe y pertenece al mismo plan
            {
                return Json(new { success = false, message = "Miembro no encontrado en tu plan" });
            }

            // Remover del plan (vuelve a plan gratuito)
            var planGratuito = await _context.Planes.FirstOrDefaultAsync(p => p.PrecioMensual == 0);
            miembro.Plan = planGratuito;
            miembro.CodigoInvitacion = null; // Limpiar código de invitación
            _context.Usuarios.Update(miembro);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Miembro expulsado exitosamente" });
        }

        // Abandonar plan grupal (MEJORADO)
        [HttpPost]
        public async Task<IActionResult> AbandonarPlan()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));// Obtener el ID del usuario logueado
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);// Buscar el usuario por ID

            if (usuario == null || usuario.Plan == null)
            {
                return Json(new { success = false, message = "No tienes un plan activo" });
            }

            // Verificar si es administrador
            var administrador = await _context.Usuarios
                .Where(u => u.Plan != null && u.Plan.Id == usuario.Plan.Id && u.CodigoInvitacion == usuario.CodigoInvitacion)// Filtrar usuarios que tienen el mismo plan y código de invitación
                .OrderBy(u => u.FechaRegistro)// Ordenar por fecha de registro para obtener al administrador
                .FirstOrDefaultAsync();// Obtener el primer usuario con este código de invitación

            if (administrador.Id == usuarioId)
            {
                return Json(new { success = false, message = "No puedes abandonar un plan que administras. Debes cancelar el plan." });
            }

            // Cambiar a plan gratuito
            var planGratuito = await _context.Planes.FirstOrDefaultAsync(p => p.PrecioMensual == 0);// Buscar el plan gratuito
            usuario.Plan = planGratuito;
            usuario.CodigoInvitacion = null; // Limpiar código de invitación
            _context.Usuarios.Update(usuario);// Actualizar el usuario en la base de datos
            await _context.SaveChangesAsync();// Guardar los cambios

            return Json(new { success = true, message = "Has abandonado el plan grupal exitosamente" });
        }

        // Cancelar plan grupal (MEJORADO)
        [HttpPost]
        public async Task<IActionResult> CancelarPlan()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));// Obtener el ID del usuario logueado
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);// Buscar el usuario por ID

            if (usuario == null || usuario.Plan == null)
            {
                return Json(new { success = false, message = "No tienes un plan activo" });
            }

            // Verificar que es el administrador
            var administrador = await _context.Usuarios
                .Where(u => u.Plan != null && u.Plan.Id == usuario.Plan.Id && u.CodigoInvitacion == usuario.CodigoInvitacion)// Filtrar usuarios que tienen el mismo plan y código de invitación
                .OrderBy(u => u.FechaRegistro)// Ordenar por fecha de registro para obtener al administrador
                .FirstOrDefaultAsync();// Obtener el primer usuario con este código de invitación

            if (administrador.Id != usuarioId)
            {
                return Json(new { success = false, message = "Solo el administrador puede cancelar el plan" });
            }

            // Obtener todos los miembros del plan con el mismo código
            var miembrosDelPlan = await _context.Usuarios
                .Where(u => u.Plan != null && u.Plan.Id == usuario.Plan.Id && u.CodigoInvitacion == usuario.CodigoInvitacion)// Filtrar usuarios que tienen el mismo plan y código de invitación
                .ToListAsync();// Obtener la lista de miembros del plan

            // Cambiar todos los miembros a plan gratuito
            var planGratuito = await _context.Planes.FirstOrDefaultAsync(p => p.PrecioMensual == 0);// Buscar el plan gratuito
            foreach (var miembro in miembrosDelPlan)// Iterar sobre cada miembro del plan
            {
                miembro.Plan = planGratuito;
                miembro.CodigoInvitacion = null; // Limpiar código de invitación
            }

            _context.Usuarios.UpdateRange(miembrosDelPlan);// Actualizar todos los miembros del plan en la base de datos
            await _context.SaveChangesAsync();// Guardar los cambios

            return Json(new { success = true, message = "Plan cancelado exitosamente. Todos los miembros han sido movidos al plan gratuito." });
        }

        // Obtener información del plan actual (MEJORADO)
        [HttpGet]
        public async Task<IActionResult> MiPlan()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));// Obtener el ID del usuario logueado
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);// Buscar el usuario por ID

            if (usuario == null)
            {
                return NotFound();
            }

            if (usuario.Plan == null)
            {
                ViewBag.Mensaje = "No tienes un plan activo.";
                return View();
            }

            // Si es plan grupal, obtener información adicional
            if (usuario.Plan.MaximoUsuarios > 1)// Verificar si el plan es grupal (más de 1 usuario)
            {
                var miembrosDelPlan = await _context.Usuarios
                    .Where(u => u.Plan != null && u.Plan.Id == usuario.Plan.Id && u.CodigoInvitacion == usuario.CodigoInvitacion)// Filtrar usuarios que tienen el mismo plan y código de invitación
                    .CountAsync();// Contar cuántos miembros hay en el plan

                var administrador = await _context.Usuarios
                    .Where(u => u.Plan != null && u.Plan.Id == usuario.Plan.Id && u.CodigoInvitacion == usuario.CodigoInvitacion)// Filtrar usuarios que tienen el mismo plan y código de invitación
                    .OrderBy(u => u.FechaRegistro)// Ordenar por fecha de registro para obtener al administrador
                    .FirstOrDefaultAsync();// Obtener el primer usuario con este código de invitación (el administrador del plan)

                ViewBag.CantidadMiembros = miembrosDelPlan;
                ViewBag.EsAdministrador = administrador?.Id == usuarioId;
                ViewBag.Administrador = administrador;
                ViewBag.CodigoInvitacion = usuario.CodigoInvitacion;
            }

            return View(usuario);
        }

        // Métodos originales mantenidos
        [HttpGet]
        public IActionResult RecargarSaldo()
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
            if (usuario == null)
                return NotFound();

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecargarSaldo(double monto)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null) return NotFound();

            usuario.Saldo = (usuario.Saldo ?? 0) + monto;
            _context.Update(usuario);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Has recargado ${monto} con éxito.";
            return RedirectToAction("Dashboard", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarPlan(int nuevoPlanId)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var usuario = await _context.Usuarios.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == usuarioId);
            var nuevoPlan = await _context.Planes.FindAsync(nuevoPlanId);

            if (usuario == null || nuevoPlan == null)
            {
                return NotFound();
            }

            // VALIDACIÓN 1: Verificar si ya tiene este plan
            if (usuario.Plan != null && usuario.Plan.Id == nuevoPlanId)
            {
                TempData["ErrorMessage"] = "Ya tienes este plan activo.";
                return RedirectToAction("Dashboard", "Home");
            }

            // VALIDACIÓN 2: Verificar saldo suficiente para planes de pago
            if (nuevoPlan.PrecioMensual > 0 && (usuario.Saldo == null || usuario.Saldo < nuevoPlan.PrecioMensual))
            {
                TempData["ErrorMessage"] = $"No tienes suficiente saldo para cambiar a este plan. Necesitas ${nuevoPlan.PrecioMensual:F2} y tienes ${usuario.Saldo ?? 0:F2}.";
                return RedirectToAction("Index");
            }

            // VALIDACIÓN 3: Prevenir downgrade de premium a gratuito
            if (usuario.Plan != null && usuario.Plan.PrecioMensual > 0 && nuevoPlan.PrecioMensual == 0)
            {
                TempData["ErrorMessage"] = "No puedes cambiar de un plan premium a un plan gratuito. Primero debes cancelar tu plan actual.";
                return RedirectToAction("Dashboard", "Home");
            }

            // VALIDACIÓN 4: Si tiene un plan premium, solo puede upgradear
            if (usuario.Plan != null && usuario.Plan.PrecioMensual > 0 && nuevoPlan.PrecioMensual > 0)
            {
                if (nuevoPlan.PrecioMensual <= usuario.Plan.PrecioMensual)
                {
                    TempData["ErrorMessage"] = "Solo puedes upgradear a un plan superior. Para cambiar a un plan del mismo nivel o inferior, primero cancela tu plan actual.";
                    return RedirectToAction("Dashboard", "Home");
                }
            }

            // PROCESAMIENTO: Descontar saldo SOLO si es un plan de pago
            if (nuevoPlan.PrecioMensual > 0)
            {
                usuario.Saldo -= nuevoPlan.PrecioMensual;
            }

            // Asignar el nuevo plan
            usuario.Plan = nuevoPlan;

            // Si es un plan grupal, generar nuevo código
            if (nuevoPlan.MaximoUsuarios > 1)
            {
                usuario.CodigoInvitacion = GenerarCodigoInvitacion();
            }
            else
            {
                // Limpiar código de invitación si no es plan grupal
                usuario.CodigoInvitacion = null;
            }

            _context.Update(usuario);
            await _context.SaveChangesAsync();

            // Mensaje de éxito
            if (nuevoPlan.PrecioMensual == 0)
            {
                TempData["SuccessMessage"] = $"¡Plan {nuevoPlan.Nombre} activado con éxito!";
            }
            else
            {
                TempData["SuccessMessage"] = $"¡Plan {nuevoPlan.Nombre} activado con éxito! Se han descontado ${nuevoPlan.PrecioMensual:F2} de tu saldo.";
            }

            return RedirectToAction("Dashboard", "Home");
        }
    }
}