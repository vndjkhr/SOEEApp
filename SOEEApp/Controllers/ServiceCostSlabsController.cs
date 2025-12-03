using SOEEApp.Models;
using System.Linq;
using System.Web.Mvc;

namespace SOEEApp.Controllers
{
    public class ServiceCostSlabsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.ServiceCostSlabs.ToList());
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(ServiceCostSlab model)
        {
            if (ModelState.IsValid)
            {
                db.ServiceCostSlabs.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }
    }
}
