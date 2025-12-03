using System;
using System.Linq;
using System.Web.Mvc;
using SOEEApp.Models;
using System.Data.Entity;

namespace SOEEApp.Controllers
{
    public class ProjectServiceMapsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // =================================================================
        // LIST MAPPINGS
        // =================================================================
        public ActionResult Index()
        {
            var list = db.ProjectServiceMaps
                .Include("Project")
                .Include("ServiceType")
                .ToList();

            return View(list);
        }

        // =================================================================
        // CREATE NEW MAPPING
        // =================================================================
        public ActionResult Create()
        {
            ViewBag.Projects = new SelectList(db.Projects, "ProjectID", "Name");
            ViewBag.ServiceTypes = new SelectList(db.ServiceTypes, "ServiceTypeID", "ServiceName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProjectServiceMap model)
        {
            if (ModelState.IsValid)
            {
                model.AddedOn = DateTime.Now;
                model.IsActive = true;

                db.ProjectServiceMaps.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Projects = new SelectList(db.Projects, "ProjectID", "Name", model.ProjectID);
            ViewBag.ServiceTypes = new SelectList(db.ServiceTypes, "ServiceTypeID", "ServiceName", model.ServiceTypeID);

            return View(model);
        }

        // =================================================================
        // MANAGE SERVICES FOR A PROJECT
        // =================================================================
        public ActionResult ManageServices(int projectId)
        {
            var project = db.Projects.FirstOrDefault(p => p.ProjectID == projectId);
            if (project == null)
                return HttpNotFound();

            var vm = new ManageServicesViewModel
            {
                Project = project,
                AllServices = db.ServiceTypes.ToList(),
                ExistingServices = db.ProjectServiceMaps
                    .Where(x => x.ProjectID == projectId)
                    .Include("ServiceType")
                    .ToList()
            };

            return View(vm);
        }

        // =================================================================
        // ADD SERVICE TO PROJECT
        // =================================================================
        [HttpPost]
        public ActionResult AddService(int projectId, int serviceTypeID)
        {
            bool exists = db.ProjectServiceMaps
                .Any(x => x.ProjectID == projectId && x.ServiceTypeID == serviceTypeID);

            if (!exists)
            {
                db.ProjectServiceMaps.Add(new ProjectServiceMap
                {
                    ProjectID = projectId,
                    ServiceTypeID = serviceTypeID,
                    AddedOn = DateTime.Now,
                    IsActive = true
                });

                db.SaveChanges();
            }

            return RedirectToAction("ManageServices", new { projectId });
        }

        // =================================================================
        // REMOVE SERVICE MAPPING
        // =================================================================
        public ActionResult Remove(int id, int projectId)
        {
            var map = db.ProjectServiceMaps.Find(id);
            if (map != null)
            {
                db.ProjectServiceMaps.Remove(map);
                db.SaveChanges();
            }

            return RedirectToAction("ManageServices", new { projectId });
        }
    }
}
