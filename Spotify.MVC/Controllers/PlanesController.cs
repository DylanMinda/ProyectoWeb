using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spotify.APIConsumer;
using Spotify.Modelos;

namespace Spotify.MVC.Controllers
{
    public class PlanesController : Controller
    {
        private const string EndPoint = "https://localhost:7028/api/Planes";
        // GET: PlanesController - Mostrar vista de selección de planes
        //public ActionResult Index()
        //{
        //    // Crear lista de planes disponibles
        //    var planes = ObtenerPlanesDisponibles();
        //    return View(planes);
        //}
        public PlanesController()
        {
            CRUD<Plan>.EndPoint = EndPoint; // Establecemos el endpoint de la API
        }

        // GET: PlanesController - Mostrar todos los planes
        public ActionResult Index()
        {
            try
            {
                var planes = CRUD<Plan>.GetAll();  // Obtener los planes desde la API
                return View(planes);  // Pasar los planes a la vista
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar los planes: " + ex.Message;
                return View();
            }
        }
        // POST: Seleccionar un plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectPlan(string planId)
        {
            try
            {
                // Aquí puedes procesar la selección del plan
                // Por ejemplo, guardar en sesión, base de datos, etc.

                // Guardar en sesión para uso posterior
                HttpContext.Session.SetString("SelectedPlan", planId);

                // Redirigir según el plan seleccionado
                switch (planId.ToLower())
                {
                    case "basic":
                        TempData["Message"] = "¡Bienvenido al plan Básico! Disfruta de música con algunas limitaciones.";
                        break;
                    case "premium":
                        TempData["Message"] = "¡Excelente elección! El plan Premium te da acceso completo.";
                        return RedirectToAction("Payment", new { planId = planId });
                    case "family":
                        TempData["Message"] = "¡Perfecto para toda la familia! Hasta 6 cuentas incluidas.";
                        return RedirectToAction("Payment", new { planId = planId });
                    case "student":
                        TempData["Message"] = "¡Aprovecha el descuento estudiantil!";
                        return RedirectToAction("StudentVerification", new { planId = planId });
                    default:
                        TempData["Error"] = "Plan no válido.";
                        return RedirectToAction("Index");
                }

                return RedirectToAction("Confirmation", new { planId = planId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al seleccionar el plan: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Confirmación de selección de plan
        public ActionResult Confirmation(string planId)
        {
            var plan = ObtenerPlanPorId(planId);
            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction("Index");
            }

            return View(plan);
        }

        // GET: Página de pago
        public ActionResult Payment(string planId)
        {
            var plan = ObtenerPlanPorId(planId);
            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction("Index");
            }

            return View(plan);
        }

        // GET: Verificación de estudiante
        public ActionResult StudentVerification(string planId)
        {
            var plan = ObtenerPlanPorId(planId);
            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction("Index");
            }

            return View(plan);
        }

        // GET: PlanesController/Details/5
        public ActionResult Details(int id)
        {
            var planes = ObtenerPlanesDisponibles();
            var plan = planes.FirstOrDefault(p => p.Id == id);

            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction("Index");
            }

            return View(plan);
        }

        // GET: PlanesController/Create - Solo para administradores
        public ActionResult Create()
        {
            return View();
        }

        // POST: PlanesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Plan plan)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Aquí agregarías la lógica para guardar en base de datos
                    // _context.Planes.Add(plan);
                    // _context.SaveChanges();

                    TempData["Success"] = "Plan creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                return View(plan);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear el plan: " + ex.Message;
                return View(plan);
            }
        }

        // GET: PlanesController/Edit/5
        public ActionResult Edit(int id)
        {
            var planes = ObtenerPlanesDisponibles();
            var plan = planes.FirstOrDefault(p => p.Id == id);

            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction("Index");
            }

            return View(plan);
        }

        // POST: PlanesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Plan plan)
        {
            try
            {
                if (id != plan.Id)
                {
                    TempData["Error"] = "ID de plan no válido.";
                    return View(plan);
                }

                if (ModelState.IsValid)
                {
                    // Aquí agregarías la lógica para actualizar en base de datos
                    // _context.Update(plan);
                    // _context.SaveChanges();

                    TempData["Success"] = "Plan actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                return View(plan);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar el plan: " + ex.Message;
                return View(plan);
            }
        }

        // GET: PlanesController/Delete/5
        public ActionResult Delete(int id)
        {
            var planes = ObtenerPlanesDisponibles();
            var plan = planes.FirstOrDefault(p => p.Id == id);

            if (plan == null)
            {
                TempData["Error"] = "Plan no encontrado.";
                return RedirectToAction("Index");
            }

            return View(plan);
        }

        // POST: PlanesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // Aquí agregarías la lógica para eliminar de base de datos
                // var plan = _context.Planes.Find(id);
                // if (plan != null)
                // {
                //     _context.Planes.Remove(plan);
                //     _context.SaveChanges();
                // }

                TempData["Success"] = "Plan eliminado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar el plan: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        #region Métodos Auxiliares

        /// <summary>
        /// Obtiene la lista de planes disponibles.
        /// En un escenario real, esto vendría de la base de datos.
        /// </summary>
        private List<Plan> ObtenerPlanesDisponibles()
        {
            return new List<Plan>
            {
                new Plan
                {
                    Id = 1,
                    Nombre = "Básico",
                    PrecioMensual = 0.00,
                    MaximoUsuarios = 1,
                    Descripcion = "Plan gratuito con acceso limitado y anuncios"
                },
                new Plan
                {
                    Id = 2,
                    Nombre = "Premium",
                    PrecioMensual = 9.99,
                    MaximoUsuarios = 1,
                    Descripcion = "Acceso completo sin anuncios, calidad HD y descargas"
                },
                new Plan
                {
                    Id = 3,
                    Nombre = "Familiar",
                    PrecioMensual = 14.99,
                    MaximoUsuarios = 6,
                    Descripcion = "Hasta 6 cuentas familiares con todas las funciones Premium"
                },
                new Plan
                {
                    Id = 4,
                    Nombre = "Estudiante",
                    PrecioMensual = 4.99,
                    MaximoUsuarios = 1,
                    Descripcion = "Descuento especial para estudiantes verificados"
                }
                
                // AGREGAR MÁS PLANES AQUÍ
                // Puedes agregar fácilmente más planes a esta lista
                /*
                ,
                new Plan
                {
                    Id = 5,
                    Nombre = "Empresarial",
                    PrecioMensual = 19.99,
                    MaximoUsuarios = 50,
                    Descripcion = "Solución completa para empresas con hasta 50 empleados"
                },
                new Plan
                {
                    Id = 6,
                    Nombre = "Premium Plus",
                    PrecioMensual = 15.99,
                    MaximoUsuarios = 1,
                    Descripcion = "Premium con funciones adicionales y audio de alta resolución"
                }
                */
            };
        }

        /// <summary>
        /// Obtiene un plan específico por su ID de string.
        /// </summary>
        private Plan ObtenerPlanPorId(string planId)
        {
            var planes = ObtenerPlanesDisponibles();

            return planId.ToLower() switch
            {
                "basic" => planes.FirstOrDefault(p => p.Nombre.ToLower() == "básico"),
                "premium" => planes.FirstOrDefault(p => p.Nombre.ToLower() == "premium"),
                "family" => planes.FirstOrDefault(p => p.Nombre.ToLower() == "familiar"),
                "student" => planes.FirstOrDefault(p => p.Nombre.ToLower() == "estudiante"),
                _ => null
            };
        }

        /// <summary>
        /// Mapea el ID de string a ID numérico.
        /// </summary>
        private int MapearPlanIdANumerico(string planId)
        {
            return planId.ToLower() switch
            {
                "basic" => 1,
                "premium" => 2,
                "family" => 3,
                "student" => 4,
                _ => 0
            };
        }

        #endregion
    }
}