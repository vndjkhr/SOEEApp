using SOEEApp.Models; // ApplicationUser, ApplicationDbContext
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.EntityFramework; // if available

namespace SOEEApp.Controllers
{
    public class ProjectsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Projects
        public ActionResult Index()
        {
            var projects = db.Projects.Include("OICUser").ToList();
            return View(projects);
        }

        // GET: Projects/Create
        public ActionResult Create()
        {
            PopulateOICDropDown();
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Project project)
        {
            if (!ModelState.IsValid)
            {
                PopulateOICDropDown();
                return View(project);
            }

            db.Projects.Add(project);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void PopulateOICDropDown()
        {
            // Option A: if you use ASP.NET Identity (default)
            var oicRole = db.Roles.FirstOrDefault(r => r.Name == "OIC");
            if (oicRole != null)
            {
                var usersInOIC = db.Users
                    .Where(u => u.Roles.Any(ur => ur.RoleId == oicRole.Id))
                    .Select(u => new { u.Id, Name = (u.Name ?? u.UserName ?? u.Email) })
                    .ToList();

                ViewBag.OICUserId = new SelectList(usersInOIC, "Id", "Name");
                return;
            }
        }
    }
}
