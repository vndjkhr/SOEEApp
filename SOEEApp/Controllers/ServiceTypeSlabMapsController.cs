using SOEEApp.Models;
using System.Linq;
using System.Web.Mvc;

namespace SOEEApp.Controllers
{
    public class ServiceTypeSlabMapsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var data = db.ServiceTypeSlabMaps
                .Include("ServiceType")
                .Include("Slab")
                .ToList();

            return View(data);
        }

        public ActionResult Create()
        {
            ViewBag.ServiceTypeID = new SelectList(db.ServiceTypes, "ServiceTypeID", "ServiceName");

            ViewBag.SlabID = db.ServiceCostSlabs
                .Select(s => new
                {
                    SlabID = s.Id,
                    Text = s.MinAmount + " - " + s.MaxAmount
                })
                .ToList();

            return View();
        }


        [HttpPost]
        public ActionResult Create(ServiceTypeSlabMap model)
        {
            if (ModelState.IsValid)
            {
                db.ServiceTypeSlabMaps.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ServiceTypeID = new SelectList(db.ServiceTypes, "ServiceTypeID", "ServiceName");
            ViewBag.SlabID = db.ServiceCostSlabs
                .Select(s => new
                {
                    SlabID = s.Id,
                    Text = s.MinAmount + " - " + s.MaxAmount
                })
                .ToList();

            return View(model);
        }
    }
}
