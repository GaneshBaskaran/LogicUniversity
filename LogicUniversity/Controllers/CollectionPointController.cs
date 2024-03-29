﻿using LogicUniversity.Context;
using LogicUniversity.Models;
using LogicUniversity.Services;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace LogicUniversity.Controllers
{
    public class CollectionPointController : Controller
    {


        private LogicUniversityContext db = new LogicUniversityContext();

        public ActionResult ManageCollection(string sessionId,string Role)
        {
            int empId = (int)Session["empId"];
            ViewData["Role"]= db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();

            if (Sessions.IsValidSession(sessionId))
            {
                //ViewData["Role"] = Role;
                ViewData["sessionId"] = sessionId;
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }



        //Selecting the new collection point
        public ActionResult UpdateCollectionPoint(string sessionId)
        {
            int empId = (int)Session["empId"];
            var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();

            //extracting the collection point details of the department whom the employee belongs to
            var collectionPoint = db.Departments.Where(r => r.DeptId == deptId).Select(r => r.CollectionLocationId).SingleOrDefault();
            var cp = db.CollectionPoints.ToList();

            //gets the current collection point name
            var currentCollectionPoint = db.CollectionPoints.Where(r => r.CollectionPointId.Equals(collectionPoint)).Select(r => r.LocationName).SingleOrDefault();

            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            ViewData["currentCollectionPoint"] = currentCollectionPoint;
            ViewData["list"] = cp;
            ViewData["deptid"] = deptId;
            ViewData["sessionId"] = sessionId;
            return View(cp);
        }



        //Updates the database Department 
        [HttpPost]
        public ActionResult SaveChangedCollectionPoint(string selection, int deptid,string sessionId)
        {
            int empId = (int)Session["empId"];
            string Role = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            Department cp = db.Departments.Where(d => d.DeptId == deptid).FirstOrDefault();
            //set to the new collectionpoint
            cp.CollectionLocationId = selection.ToString();
            db.SaveChanges();
            TempData["successmsg"] = "Successfully changed the collection point";
            return RedirectToAction("Display", "CollectionPoint",new { sessionId = sessionId,Role=Role});
        }




        public ActionResult UpdateRepresentative(string sessionId)
        {
            int empId = (int)Session["empId"];
            var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            //extracting the dep rep details of the department whom the employee belongs to
            var currentRepresentative = db.Employees.Where(r => r.DeptId == deptId && r.Role == "DEP_REP").Select(x => x.EmployeeName).SingleOrDefault();

            ViewData["representative"] = currentRepresentative;

            //Only taking employees from the database,thereby excluding HOD and others
            var departmentRepresentatives = db.Employees.Where(r => r.DeptId == deptId && r.Role == "DEP_STAFF");

            ViewBag.EmployeeId = new SelectList(departmentRepresentatives, "EmployeeId", "EmployeeName");

            ViewData["deptid"] = deptId;
            ViewData["sessionId"] = sessionId;
            return View(departmentRepresentatives.ToList());
        }



        //Saving to database Employee
        [HttpPost]
        public ActionResult SaveChangedDepartmentrepresentative(Employee emp, string name,string sessionId)
        {
            int empId = (int)Session["empId"];
            var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();
            var rep = db.Employees.Where(r => r.DeptId == deptId && r.Role == "DEP_REP").SingleOrDefault();
            var department = db.Departments.Find(deptId);
            string Roles = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            if (ModelState.IsValid)
            {
                rep.Role = "DEP_STAFF";
                Employee newrep = db.Employees.Find(emp.EmployeeId);
                newrep.Role = "DEP_REP";
                department.ContactName = newrep.EmployeeName;
                db.SaveChanges();
                TempData["successmsg"] = "Successfully changed the representative";
                return RedirectToAction("Display", "CollectionPoint",new { sessionId=sessionId,Role=Roles});
            }
            return RedirectToAction("UpdateRepresentative", "CollectionPoint", new { sessionId = @sessionId });


        }

        public ActionResult Display(string sessionId,string Role)
        {
            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                ViewData["Role"] = Role;

                int empId = (int)Session["empId"];
                var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();
                var currentRepresentative = db.Employees.Where(r => r.DeptId == deptId && r.Role == "DEP_REP").Select(x => x.EmployeeName).SingleOrDefault();
                var collectionPoint = db.Departments.Where(r => r.DeptId == deptId).Select(r => r.CollectionLocationId).SingleOrDefault();
                var currentCollectionPoint = db.CollectionPoints.Where(r => r.CollectionPointId.Equals(collectionPoint)).Select(r => r.LocationName).SingleOrDefault();
                ViewData["rep"] = currentRepresentative;
                ViewData["cp"] = currentCollectionPoint;
                ViewBag.Message = TempData["successmsg"];
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }





        //store
        // GET: CollectionPoints
        public ActionResult ViewCollectionPoint(string sessionId)
        {
            if (Sessions.IsValidSession(sessionId))
            {
                int empId = (int)Session["empId"];
                ViewData["role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
                ViewData["sessionId"] = sessionId;
                var collectionPoints = db.CollectionPoints.Include(c => c.Employee);
                return View(collectionPoints.ToList());
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }

        // GET: CollectionPoints/Details/5
        public ActionResult GetCollectionPointDetails(string id,string sessionId)
        {
            int empId = (int)Session["empId"];
            ViewData["role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                CollectionPoint collectionPoint = (db.CollectionPoints).Include(d=>d.Employee).Where(d=>d.CollectionPointId==id).FirstOrDefault();

                if (collectionPoint == null)
                {
                    return HttpNotFound();
                }
                return View(collectionPoint);
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }

        // GET: CollectionPoints/Create
        public ActionResult CreateNewCollectionPoint(string sessionId)
        {
            int empId = (int)Session["empId"];
            ViewData["role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                ViewBag.StoreClerkId = new SelectList(db.Employees.Where(x => x.Role == "STORE_CLRK"), "EmployeeId", "EmployeeName");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }

        // POST: CollectionPoints/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateNewCollectionPoint([Bind(Include = "CollectionPointId,LocationName,Time,Day,StoreClerkId")] CollectionPoint collectionPoint,string sessionId)
        {
            int empId = (int)Session["empId"];
            ViewData["role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            if (sessionId == null)
            {
                sessionId = Request["sessionId"];
            }
            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                if (ModelState.IsValid)
                {
                    db.CollectionPoints.Add(collectionPoint);
                    db.SaveChanges();
                    return RedirectToAction("ViewCollectionPoint",new { sessionId = sessionId });
                }

                ViewBag.StoreClerkId = new SelectList(db.Employees, "EmployeeId", "EmployeeName", collectionPoint.StoreClerkId);
                return View(collectionPoint);
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }

        // GET: CollectionPoints/Edit/5
        public ActionResult EditCollectionPointDetails(string id,string sessionId)
        {
            if (Sessions.IsValidSession(sessionId))
            {
                int empId = (int)Session["empId"];
                ViewData["role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
                ViewData["sessionId"] = sessionId;
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                CollectionPoint collectionPoint = db.CollectionPoints.Find(id);
                Employee emp = db.Employees.Find(collectionPoint.StoreClerkId);

                if (collectionPoint == null)
                {
                    return HttpNotFound();
                }
                ViewData["Employee"] = emp;
                ViewBag.StoreClerkId = new SelectList(db.Employees.Where(x => x.Role == "STORE_CLRK"), "EmployeeId", "EmployeeName", collectionPoint.StoreClerkId);
                return View(collectionPoint);
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }

        // POST: CollectionPoints/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCollectionPointDetails([Bind(Include = "CollectionPointId,LocationName,Time,Day,StoreClerkId")] CollectionPoint collectionPoint,string sessionId)
        {
            int empId = (int)Session["empId"];
            ViewData["role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            if (sessionId == null)
            {
                sessionId = Request["sessionId"];
            }

            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                if (ModelState.IsValid)
                {
                    db.Entry(collectionPoint).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("ViewCollectionPoint",new { sessionId = sessionId });
                }
                ViewBag.StoreClerkId = new SelectList(db.Employees, "EmployeeId", "EmployeeName", collectionPoint.StoreClerkId);
                return View(collectionPoint);
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }

        // GET: CollectionPoints/Delete/5
        public ActionResult DeleteCollectionPoint(string id,string sessionId)
        {
            int empId = (int)Session["empId"];
            ViewData["role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            ViewData["id"] = id;
            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                CollectionPoint collectionPoint = db.CollectionPoints.Find(id);
                if (collectionPoint == null)
                {
                    return HttpNotFound();
                }
                return View(collectionPoint);
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }

        // POST: CollectionPoints/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id,string sessionId)
        {
          
            if (sessionId == null)
            {
                sessionId = Request["sessionId"];
            }

            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                CollectionPoint collectionPoint = db.CollectionPoints.Find(id);
                db.CollectionPoints.Remove(collectionPoint);
                db.SaveChanges();
                return RedirectToAction("ViewCollectionPoint", new { sessionId = sessionId });
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}