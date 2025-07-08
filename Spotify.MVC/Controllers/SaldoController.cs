using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Spotify.MVC.Controllers
{
    public class SaldoController : Controller
    {
        // GET: SaldoController
        public ActionResult Index()
        {
            return View();
        }

        // GET: SaldoController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SaldoController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SaldoController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SaldoController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: SaldoController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SaldoController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SaldoController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
