using System;
using System.Linq;
using System.Web.Mvc;
using SOEEApp.Models;
using SOEEApp.Models.ViewModels;
using SOEEApp.Helpers;
using System.Collections.Generic;
using System.Data.Entity; // Make sure to include this

namespace SOEEApp.Controllers
{
    public class SOEEController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: SOEE
        public ActionResult Index(int projectId = 0)
        {
            var list = db.SOEEs
                         .Include(s => s.Project)
                         .Include(s => s.Customer)
                         .Where(s => projectId == 0 || s.ProjectID == projectId)
                         .OrderByDescending(s => s.SOEERaiseDate)
                         .ToList();

            return View(list);
        }

        // GET: SOEE/Create
        public ActionResult Create(int? projectId = null)
        {
            var vm = new SOEECreateViewModel();

            ViewBag.Projects = new SelectList(db.Projects.ToList(), "ProjectID", "Name");
            ViewBag.Customers = new SelectList(db.Customers.ToList(), "CustomerID", "CustomerName");

            if (projectId.HasValue)
                vm.ProjectID = projectId.Value;

            ViewBag.ServiceTypes = db.ServiceTypes.ToList();

            return View(vm);
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SOEECreateViewModel vm)
        {
            // Validate items present
            vm.Items = vm.Items?.Where(x => x != null && !x.IsDeleted && x.ServiceTypeID > 0).ToList()
                      ?? new List<SOEEItemViewModel>();
            if (vm.Items.Count == 0)
            {
                ModelState.AddModelError("", "Please add at least one valid item.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Projects = new SelectList(db.Projects.ToList(), "ProjectID", "Name", vm.ProjectID);
                ViewBag.Customers = new SelectList(db.Customers.ToList(), "CustomerID", "CustomerName", vm.CustomerID);
                ViewBag.ServiceTypes = db.ServiceTypes.ToList();
                return View(vm);
            }

            // Create SOEE header
            var soee = new SOEE
            {
                ProjectID = vm.ProjectID,
                CustomerID = vm.CustomerID,
                ReferenceNo = vm.ReferenceNo,
                SOEERaiseDate = vm.SOEERaiseDate,
                MarkTo = vm.MarkTo,
                Subject = vm.Subject,
                Content = vm.Content,
                Reference = vm.Reference,
                CreatedBy = User.Identity.Name,
                Status = SOEEStatus.Submitted,
                CreatedDate = DateTime.Now,
                Items = new List<SOEEItem>()
            };

            // Map new items. For new items, ensure ServiceChargePercent = 0 so calculator computes and assigns it.
            foreach (var i in vm.Items)
            {
                soee.Items.Add(new SOEEItem
                {
                    ServiceTypeID = i.ServiceTypeID,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    DescriptionOfWork = i.DescriptionOfWork,

                    // force calculation in CostCalculator
                    SubTotal = i.Quantity * i.Unit * i.UnitPrice,
                    ServiceChargePercent = 0m,
                    ServiceCharge = 0m,
                    CGST = 0m,
                    SGST = 0m,
                    Total = i.SubTotal + i.ServiceCharge + i.CGST + i.SGST,

                    IsDeleted = false
                });
            }

            // Save master to generate SOEEID (if you need SOEEID for any reason)
            db.SOEEs.Add(soee);
            db.SaveChanges();

            // Compute and persist per-item values and totals
            var activeItems = soee.Items.Where(x => !x.IsDeleted).ToList();
            var totals = CostCalculator.ComputeTotalsForItems(activeItems, db, soee.ProjectID, soee.SOEEID);

            soee.TotalBasicAmount = totals.Basic;
            soee.TotalServiceCharge = totals.ServiceCharge;
            soee.TotalTaxAmount = totals.CGST + totals.SGST;
            soee.GrandTotal = totals.Total;

            db.SaveChanges();

            return RedirectToAction("Index");
        }


        // GET: SOEE/Edit/5
        public ActionResult Edit(int id)
        {
            var soee = db.SOEEs
                         .Include("Items")
                         .FirstOrDefault(s => s.SOEEID == id);

            if (soee == null) return HttpNotFound();

            var vm = new SOEECreateViewModel
            {
                SOEEID = soee.SOEEID,
                ProjectID = soee.ProjectID,
                CustomerID = soee.CustomerID,
                ReferenceNo = soee.ReferenceNo,
                SOEERaiseDate = soee.SOEERaiseDate,
                MarkTo = soee.MarkTo,
                Subject = soee.Subject,
                Reference = soee.Reference,
                Content = soee.Content,

                // -------------------------------
                // LOAD ONLY ACTIVE ITEMS
                // -------------------------------
                Items = soee.Items
                    .Where(it => !it.IsDeleted)
                    .Select(it => new SOEEItemViewModel
                    {
                        SOEEItemID = it.SOEEItemID,
                        DescriptionOfWork = it.DescriptionOfWork,
                        ServiceTypeID = it.ServiceTypeID,
                        Unit = it.Unit,
                        Quantity = it.Quantity,
                        UnitPrice = it.UnitPrice,

                        // Service charge tracking // UI-calculated values
                        SubTotal = it.Quantity * it.Unit * it.UnitPrice,
                        ServiceChargePercent = it.ServiceChargePercent,  // <---- ADDED
                        ServiceCharge = it.ServiceCharge,
                        CGST = it.CGST,
                        SGST = it.SGST,
                        Total = it.SubTotal + it.ServiceCharge + it.CGST + it.SGST,
                        // Important for edit delete flow
                        IsDeleted = it.IsDeleted                        // <---- ADDED
                    }).ToList()
            };

            // Dropdowns
            ViewBag.Projects = new SelectList(db.Projects.ToList(), "ProjectID", "Name", vm.ProjectID);
            ViewBag.Customers = new SelectList(db.Customers.ToList(), "CustomerID", "CustomerName", vm.CustomerID);
            ViewBag.ServiceTypes = db.ServiceTypes.ToList();

            return View("Edit", vm);
        }


        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SOEECreateViewModel vm)
        {
            // filter posted items
            vm.Items = vm.Items?.Where(x => x != null).ToList() ?? new List<SOEEItemViewModel>();

            if (vm.Items.Count == 0)
            {
                ModelState.AddModelError("", "Please add at least one valid item.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Projects = new SelectList(db.Projects.ToList(), "ProjectID", "Name", vm.ProjectID);
                ViewBag.Customers = new SelectList(db.Customers.ToList(), "CustomerID", "CustomerName", vm.CustomerID);
                ViewBag.ServiceTypes = db.ServiceTypes.ToList();
                return View(vm);
            }

            var soee = db.SOEEs.Include("Items").FirstOrDefault(s => s.SOEEID == vm.SOEEID);
            if (soee == null) return HttpNotFound();

            // update header
            soee.ProjectID = vm.ProjectID;
            soee.CustomerID = vm.CustomerID;
            soee.ReferenceNo = vm.ReferenceNo;
            soee.SOEERaiseDate = vm.SOEERaiseDate;
            soee.MarkTo = vm.MarkTo;
            soee.Subject = vm.Subject;
            soee.Content = vm.Content;
            soee.Reference = vm.Reference;

            // soft-delete all existing by default (we will restore/update those present in vm.Items)
            foreach (var old in soee.Items)
                old.IsDeleted = true;

            // process posted items
            foreach (var i in vm.Items)
            {
                if (i.SOEEItemID > 0)
                {
                    // existing item — find it
                    var existing = soee.Items.FirstOrDefault(x => x.SOEEItemID == i.SOEEItemID);
                    if (existing != null)
                    {
                        if (i.IsDeleted)
                        {
                            // keep it soft-deleted
                            existing.IsDeleted = true;
                            continue; // skip rest
                        }

                        // update existing item
                        existing.DescriptionOfWork = i.DescriptionOfWork;
                        existing.ServiceTypeID = i.ServiceTypeID;
                        existing.Unit = i.Unit;
                        existing.Quantity = i.Quantity;
                        existing.UnitPrice = i.UnitPrice;

                        existing.ServiceChargePercent = i.ServiceChargePercent;

                        // MUST RECALCULATE
                        existing.SubTotal = existing.Quantity * existing.Unit * existing.UnitPrice;
                        existing.ServiceCharge = (existing.SubTotal * existing.ServiceChargePercent) / 100;
                        var taxable = existing.SubTotal + existing.ServiceCharge;
                        existing.CGST = taxable * 0.09m;
                        existing.SGST = taxable * 0.09m;
                        existing.Total = existing.SubTotal + existing.ServiceCharge + existing.CGST + existing.SGST;

                        existing.IsDeleted = false;
                    }
                }
                else
                {
                    // new item
                    if (i.IsDeleted) continue; // skip deleted new rows

                    var subTotal = i.Quantity * i.Unit * i.UnitPrice;
                    var serviceCharge = (subTotal * i.ServiceChargePercent) / 100;
                    var taxable = subTotal + serviceCharge;

                    var cgst = taxable * 0.09m;
                    var sgst = taxable * 0.09m;

                    soee.Items.Add(new SOEEItem
                    {
                        DescriptionOfWork = i.DescriptionOfWork,
                        ServiceTypeID = i.ServiceTypeID,
                        Unit = i.Unit,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,

                        ServiceChargePercent = i.ServiceChargePercent,
                        ServiceCharge = serviceCharge,

                        SubTotal = subTotal,
                        CGST = cgst,
                        SGST = sgst,
                        Total = subTotal + serviceCharge + cgst + sgst,

                        IsDeleted = false
                    });
                }
            }


            // Now compute totals and update item fields (ServiceChargePercent for new/changed items)
            var activeItems = soee.Items.Where(x => !x.IsDeleted).ToList();

            var totals = CostCalculator.ComputeTotalsForItems(activeItems, db, soee.ProjectID, soee.SOEEID);

            soee.TotalBasicAmount = totals.Basic;
            soee.TotalServiceCharge = totals.ServiceCharge;
            soee.TotalTaxAmount = totals.CGST + totals.SGST;
            soee.GrandTotal = totals.Total;

            db.SaveChanges();

            return RedirectToAction("Details", new { id = soee.SOEEID });
        }

        public ActionResult Details(int id)
        {
            var soee = db.SOEEs
                         .Include(s => s.Items.Select(it => it.ServiceType))
                         .Include(s => s.Project)
                         .Include(s => s.Customer)
                         .FirstOrDefault(s => s.SOEEID == id);

            if (soee == null) return HttpNotFound();

            var vm = new SOEECreateViewModel
            {
                SOEEID = soee.SOEEID,
                ProjectID = soee.ProjectID,
                CustomerID = soee.CustomerID,
                ReferenceNo = soee.ReferenceNo,
                MarkTo = soee.MarkTo,
                Subject = soee.Subject,
                Reference = soee.Reference,
                Content = soee.Content,
                SOEERaiseDate = soee.SOEERaiseDate,
                TotalBasicAmount = soee.TotalBasicAmount,
                TotalServiceCharge = soee.TotalServiceCharge,
                TotalTaxAmount = soee.TotalTaxAmount,
                GrandTotal = soee.GrandTotal,
                Project = soee.Project,
                Customer = soee.Customer,
                Items = soee.Items
                    .Where(it => !it.IsDeleted)
                    .Select(i => new SOEEItemViewModel
                    {
                        SOEEItemID = i.SOEEItemID,
                        DescriptionOfWork = i.DescriptionOfWork,
                        ServiceTypeID = i.ServiceTypeID,
                        ServiceTypeName = i.ServiceType.ServiceName,
                        Unit = i.Unit,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        SubTotal = i.SubTotal,
                        ServiceChargePercent = i.ServiceChargePercent, // Use DB value
                        ServiceCharge = i.ServiceCharge,
                        TotalAfterServiceCharge = i.SubTotal + i.ServiceCharge,
                        CGST = i.CGST,
                        SGST = i.SGST,
                        Total = i.Total
                    })
                    .OrderBy(it => it.SOEEItemID) // optional: keeps items in insertion order
                    .ToList()
            };

            return View(vm);
        }



        [HttpPost]
        public JsonResult GetServiceChargePercent(int serviceTypeId, decimal subtotal, int projectId)
        {
            using (var db = new ApplicationDbContext())
            {
                decimal previousTotal = 0;//write code for previous soee pendings

                decimal cumulative = previousTotal + subtotal;

                // Find matching slab
                var percent =
                    (from m in db.ServiceTypeSlabMaps
                     where m.ServiceTypeID == serviceTypeId
                        && cumulative >= m.Slab.MinAmount
                        && cumulative <= m.Slab.MaxAmount
                     select (decimal?)m.Percentage)
                     .FirstOrDefault() ?? 0;

                return Json(new { percentage = percent });
            }
        }

        public class ItemDto { public int serviceTypeID { get; set; } public decimal qty { get; set; } public decimal amount { get; set; } }

        [HttpPost]
        public JsonResult AjaxCompute(int projectID, ItemDto[] items)
        {
            // ItemDto: { int serviceTypeID; decimal qty; decimal amount; }
            var soee = new SOEE
            {
                ProjectID = projectID,
                Items = items.Select(i => new SOEEItem { ServiceTypeID = i.serviceTypeID, Quantity = i.qty, UnitPrice = (i.qty == 0 ? 0 : i.amount / i.qty) }).ToList()
            };

            var activeItems = soee.Items.Where(x => !x.IsDeleted).ToList();
            var totals = CostCalculator.ComputeTotalsForItems(activeItems, db, soee.ProjectID, soee.SOEEID);
            return Json(new { serviceCharge = totals.ServiceCharge, cgst = totals.CGST, sgst = totals.SGST, total = totals.Total });
        }

    }
}