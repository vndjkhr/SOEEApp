using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SOEEApp.Models; // Adjust namespace accordingly

public class ServiceTypesController : Controller
{
    private ApplicationDbContext db = new ApplicationDbContext();

    // GET: ServiceTypes
    public ActionResult Index()
    {
        var list = db.ServiceTypes.ToList();
        return View(list);
    }

    // GET: ServiceTypes/Create
    public ActionResult Create()
    {
        return View();
    }

    // POST: ServiceTypes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(ServiceType serviceType)
    {
        if (ModelState.IsValid)
        {
            serviceType.CreatedDate = DateTime.Now;
            db.ServiceTypes.Add(serviceType);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        return View(serviceType);
    }

    // GET: ServiceTypes/Edit/5
    public ActionResult Edit(int? id)
    {
        if (id == null)
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

        var service = db.ServiceTypes.Find(id);
        if (service == null)
            return HttpNotFound();

        return View(service);
    }

    // POST: ServiceTypes/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(ServiceType serviceType)
    {
        if (ModelState.IsValid)
        {
            db.Entry(serviceType).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        return View(serviceType);
    }

    // GET: ServiceTypes/Delete/5
    public ActionResult Delete(int id)
    {
        var service = db.ServiceTypes.Find(id);
        if (service == null)
            return HttpNotFound();

        db.ServiceTypes.Remove(service);
        db.SaveChanges();

        return RedirectToAction("Index");
    }
}
