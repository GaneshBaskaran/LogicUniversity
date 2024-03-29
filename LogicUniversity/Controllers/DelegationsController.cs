﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PagedList;
using PagedList.Mvc;
using LogicUniversity.Context;
using LogicUniversity.Models;
using LogicUniversity.Services;

namespace LogicUniversity.Controllers
{
    public class DelegationsController : Controller
    {
        private LogicUniversityContext db = new LogicUniversityContext();

        // GET: Delegations

        public ActionResult ManageDelegation(string sessionId)
        {
            int empId = (int)Session["empId"];
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }


        public ActionResult ViewDelegation(int ?page,string sessionId)
        {
            ViewData["sessionId"] = sessionId;
            int empId = (int)Session["empId"];
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();
            var delegations = db.Delegations.Include(d => d.Employee).Where(d=>d.Employee.DeptId==deptId).OrderByDescending(d=>d.StartDate);
            ViewData["msg"] =null;
            ViewBag.Message = TempData["toastmessage"];
            return View(delegations.ToPagedList(page ?? 1, 5));
        }

        // GET: Delegations/Details/5
        public ActionResult GetDelegationDetails(int? id, string sessionId)
        {
            int empId = (int)Session["empId"];
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Delegation delegation = db.Delegations.Include(d=>d.Employee).Where(d=>d.DelegationId==id).FirstOrDefault();
                if (delegation == null)
                {
                    return HttpNotFound();
                }
                return View(delegation);
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }

        // GET: Delegations/Create
        public ActionResult AppointDelegation(string sessionId)
        {
            ViewData["sessionId"] = sessionId;
            int empId = (int)Session["empId"];
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.Role == "DEP_STAFF" && d.DeptId == deptId), "EmployeeId", "EmployeeName");
            ViewData["list"] = null;
            return View();
        }

      

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AppointDelegation([Bind(Include = "DelegationId,EmployeeId,StartDate,EndDate")] Delegation delegation,string sessionId)
        {
            ViewData["sessionId"] = sessionId;
            int empId = (int)Session["empId"];
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            DelegationService ds = new DelegationService();
            List<string> msglist = new List<string>();

            if (ModelState.IsValid)
            {
                var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.Role == "DEP_STAFF" && d.DeptId == deptId), "EmployeeId", "EmployeeName");
                int value = DateTime.Compare(delegation.StartDate, delegation.EndDate);
                if (value > 0 || delegation.StartDate < DateTime.Today)
                {
                    if(value>0)
                    {
                        msglist.Add("Enddate must follow Startdate");
                    }
                    else if(delegation.StartDate < DateTime.Today)
                    {
                        msglist.Add("Select date preceeds today's date");          
                    }
                    ViewData["list"] = msglist;
                    return View("AppointDelegation");
                }
                else
                {
                    
                    var case1 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate >= delegation.StartDate && d.EndDate >= delegation.EndDate && d.Employee.DeptId == deptId).ToList();
                    var case2 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate <= delegation.StartDate && d.EndDate <= delegation.EndDate && d.Employee.DeptId == deptId).ToList();
                    var case3 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate >= delegation.StartDate && d.EndDate <= delegation.EndDate && d.Employee.DeptId == deptId).ToList();
                    var case4 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate < delegation.StartDate && d.EndDate > delegation.EndDate && d.Employee.DeptId == deptId).ToList();
        
                    if (case1.Count == 0 && case2.Count == 0 && case3.Count == 0 && case4.Count == 0)
                    {
                        ds.AddDelegation(delegation, empId);
                        TempData["toastmessage"] = "Delegation Appointment Successful";
                        return RedirectToAction("ViewDelegation",new { sessionId=sessionId});
                    }
                    else
                    {

                        if (case3.Count != 0)
                        {

                            foreach (var item in case3)
                            {
                                List<string>msg = ds.CaptureErrorMsg(item);
                                msglist.Add(msg[0] + " is deleagated from " + msg[1] + " to" + msg[2]);
                            }
                            ViewData["list"] = msglist;
                            return View("AppointDelegation");
                        }

                        else if (case4.Count != 0)
                        {

                            foreach (var item in case4)
                            {
                                List<string> msg = ds.CaptureErrorMsg(item);
                                msglist.Add(msg[0] + " is deleagated from " + msg[1] + " to" + msg[2]);
                            }
                            ViewData["list"] = msglist;
                            return View("AppointDelegation");
                        }
                  
                        else if (case1.Count != 0)
                        {
                            int flag = 0;
                            foreach (var item in case1)
                            {
                                if (item.StartDate > delegation.EndDate)
                                {
                                    flag++;
                                }
                                else
                                {
                                    List<string> msg = ds.CaptureErrorMsg(item);
                                    msglist.Add(msg[0] + " is deleagated from " + msg[1] + " to" + msg[2]);
                                }

                            }
                            if(flag==case1.Count&&case2.Count==0)
                            {
                                ds.AddDelegation(delegation, empId);
                                TempData["toastmessage"] = "Delegation Appointment Successful";
                                return RedirectToAction("ViewDelegation",new { sessionId=sessionId});
                            }
                            else if(flag == case1.Count && case2.Count != 0)
                            {
                                int flag1 = 0;
                                foreach (var item in case2)
                                {
                                    if (item.EndDate < delegation.StartDate)
                                    {
                                        flag1++;
                                    }
                                    else
                                    {
                                        List<string> msg = ds.CaptureErrorMsg(item);
                                        msglist.Add(msg[0] + " is  deleagated from " + msg[1] + " to" + msg[2]);
                                    }

                                }
                                if (flag1 == case2.Count)
                                {
                                    ds.AddDelegation(delegation, empId);
                                    TempData["toastmessage"] = "Delegation Appointment Successful";
                                    return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                                }
                                else
                                {
                                    ViewData["list"] = msglist;
                                    return View("AppointDelegation");
                                }


                            }
                            else
                            {
                                ViewData["list"] = msglist;
                                return View("AppointDelegation");
                            }
                          
                        }

                        else if (case2.Count != 0)
                        {
                            int flag = 0;
                            foreach (var item in case2)
                            {
                                if (item.EndDate < delegation.StartDate)
                                {
                                    flag++;       
                                }
                                else
                                {
                                    List<string> msg = ds.CaptureErrorMsg(item);
                                    msglist.Add(msg[0] + " is  deleagated from " + msg[1] + " to" + msg[2]);
                                }
                                
                            }
                            if (flag == case2.Count)
                            {
                                ds.AddDelegation(delegation, empId);
                                TempData["toastmessage"] = "Delegation Appointment Successful";
                                return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                            }
                            else
                            {
                                ViewData["list"] = msglist;
                                return View("AppointDelegation");
                            }
                        
                        }
                       
                    }

                }

            }
            ViewBag.EmployeeId = new SelectList(db.Employees, "EmployeeId", "EmployeeName", delegation.EmployeeId);
            return View(delegation);
        }




       

        // GET: Delegations/Edit/5
        public ActionResult EditDelegation(int? id,string sessionId)
        {
            int empId = (int)Session["empId"];
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            ViewData["sessionId"] = sessionId;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Delegation delegation = db.Delegations.Find(id);
            var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();
            Employee emp = db.Employees.Find(delegation.EmployeeId);

            if (delegation == null)
            {
                return HttpNotFound();
            }
            if (emp.DeptId == deptId && !(delegation.EndDate < DateTime.Today))
            {
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(d=>d.Department.DeptId==deptId&&d.Role== "DEP_STAFF"), "EmployeeId", "EmployeeName", delegation.EmployeeId);
                return View(delegation);
            }
            else
            {
                return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
            }
        }




        // POST: Delegations/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditDelegation([Bind(Include = "DelegationId,EmployeeId,StartDate,EndDate")] Delegation delegation,string sessionId)
        {
            ViewData["sessionId"] = sessionId;
            int empId = (int)Session["empId"];
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();
            DelegationService ds = new DelegationService();
            List<string> msglist = new List<string>();
            if (ModelState.IsValid)
            {

                Delegation originalDelegation = db.Delegations.Find(delegation.DelegationId);
                if (originalDelegation.StartDate < DateTime.Today)
                {
                    if (delegation.StartDate != originalDelegation.StartDate)
                    {
                        msglist.Add("Start Date cant be changed");
                        ViewData["list"] = msglist;
                        return RedirectToAction("EditDelegation", new { sessionId = sessionId });
                    }
                    
                    else
                    {

                        ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.Role == "DEP_STAFF" && d.DeptId == deptId), "EmployeeId", "EmployeeName");
                        int value = DateTime.Compare(delegation.StartDate, delegation.EndDate);
                        if (value > 0 || delegation.EndDate < DateTime.Today)
                        {
                            if (value > 0)
                            {
                                msglist.Add("Enddate must follow Startdate");
                            }
                            else if (delegation.EndDate < DateTime.Today)
                            {
                                msglist.Add("Enddate must follow today's date");
                            }

                            ViewData["list"] = msglist;
                            return View("EditDelegation");
                        }
                        else
                        {

                            var case1 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate >= delegation.StartDate && d.EndDate >= delegation.EndDate && d.Employee.DeptId == deptId && d.DelegationId != delegation.DelegationId).ToList();
                            var case2 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate <= delegation.StartDate && d.EndDate <= delegation.EndDate && d.Employee.DeptId == deptId && d.DelegationId != delegation.DelegationId).ToList();
                            var case3 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate >= delegation.StartDate && d.EndDate <= delegation.EndDate && d.Employee.DeptId == deptId && d.DelegationId != delegation.DelegationId).ToList();
                            var case4 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate < delegation.StartDate && d.EndDate > delegation.EndDate && d.Employee.DeptId == deptId && d.DelegationId != delegation.DelegationId).ToList();

                            if (case1.Count == 0 && case2.Count == 0 && case3.Count == 0 && case4.Count == 0)
                            {
                                ds.saveDelegationChanges(delegation,empId);
                                TempData["toastmessage"] = "Successfully edited delegation details";
                                return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                            }
                            else
                            {

                                if (case3.Count != 0)
                                {

                                    foreach (var item in case3)
                                    {
                                        List<string> msg = ds.CaptureErrorMsg(item);
                                        msglist.Add(msg[0] + " is deleagated from " + msg[1] + " to" + msg[2]);
                                    }
                                    ViewData["list"] = msglist;
                                    return View("EditDelegation");
                                }

                                else if (case4.Count != 0)
                                {

                                    foreach (var item in case4)
                                    {
                                        List<string> msg = ds.CaptureErrorMsg(item);
                                        msglist.Add(msg[0] + " is deleagated from " + msg[1] + " to" + msg[2]);
                                    }
                                    ViewData["list"] = msglist;
                                    return View("EditDelegation");
                                }

                                else if (case1.Count != 0)
                                {
                                    int flag = 0;
                                    foreach (var item in case1)
                                    {
                                        if (item.StartDate > delegation.EndDate)
                                        {
                                            flag++;
                                        }
                                        else
                                        {
                                            List<string> msg = ds.CaptureErrorMsg(item);
                                            msglist.Add(msg[0] + " is deleagated from " + msg[1] + " to" + msg[2]);
                                        }

                                    }
                                    if (flag == case1.Count && case2.Count == 0)
                                    {
                                        ds.saveDelegationChanges(delegation, empId);
                                        TempData["toastmessage"] = "Successfully edited delegation details";
                                        return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                                    }
                                    else if (flag == case1.Count && case2.Count != 0)
                                    {
                                        int flag1 = 0;
                                        foreach (var item in case2)
                                        {
                                            if (item.EndDate < delegation.StartDate)
                                            {
                                                flag1++;
                                            }
                                            else
                                            {
                                                List<string> msg = ds.CaptureErrorMsg(item);
                                                msglist.Add(msg[0] + " is  deleagated from " + msg[1] + " to" + msg[2]);
                                            }

                                        }
                                        if (flag1 == case2.Count)
                                        {
                                            ds.saveDelegationChanges(delegation, empId);
                                            TempData["toastmessage"] = "Successfully edited delegation details";
                                            return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                                        }
                                        else
                                        {
                                            ViewData["list"] = msglist;
                                            return View("EditDelegation");
                                        }


                                    }
                                    else
                                    {
                                        ViewData["list"] = msglist;
                                        return View("EditDelegation");
                                    }

                                }

                                else if (case2.Count != 0)
                                {
                                    int flag = 0;
                                    foreach (var item in case2)
                                    {
                                        if (item.EndDate < delegation.StartDate)
                                        {
                                            flag++;
                                        }
                                        else
                                        {
                                            List<string> msg = ds.CaptureErrorMsg(item);
                                            msglist.Add(msg[0] + " is  deleagated from " + msg[1] + " to" + msg[2]);
                                        }

                                    }
                                    if (flag == case2.Count)
                                    {
                                        ds.saveDelegationChanges(delegation, empId);
                                        TempData["toastmessage"] = "Successfully edited delegation details";
                                        return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                                    }
                                    else
                                    {
                                        ViewData["list"] = msglist;
                                        return View("EditDelegation");
                                    }

                                }

                            }

                        }
                    }

                }

                else
                {
                    ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.Role == "DEP_STAFF" && d.DeptId == deptId), "EmployeeId", "EmployeeName");
                    int value = DateTime.Compare(delegation.StartDate, delegation.EndDate);
                    if (value > 0 || delegation.EndDate < DateTime.Today)
                    {
                        if (value > 0)
                        {
                            msglist.Add("Enddate must follow Startdate");
                        }
                        else if (delegation.EndDate < DateTime.Today)
                        {
                            msglist.Add("Enddate must follow today's date");
                        }

                        ViewData["list"] = msglist;
                        return View("EditDelegation");
                    }
                    if (delegation.StartDate < DateTime.Today)
                    {
                        msglist.Add("Please check the start date");
                        ViewData["list"] = msglist;
                        return View("EditDelegation");
                    }
                    else
                    {

                        var case1 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate >= delegation.StartDate && d.EndDate >= delegation.EndDate && d.Employee.DeptId == deptId && d.DelegationId != delegation.DelegationId).ToList();
                        var case2 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate <= delegation.StartDate && d.EndDate <= delegation.EndDate && d.Employee.DeptId == deptId && d.DelegationId != delegation.DelegationId).ToList();
                        var case3 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate >= delegation.StartDate && d.EndDate <= delegation.EndDate && d.Employee.DeptId == deptId && d.DelegationId != delegation.DelegationId).ToList();
                        var case4 = db.Delegations.Include(d => d.Employee).Where(d => d.StartDate < delegation.StartDate && d.EndDate > delegation.EndDate && d.Employee.DeptId == deptId && d.DelegationId != delegation.DelegationId).ToList();

                        if (case1.Count == 0 && case2.Count == 0 && case3.Count == 0 && case4.Count == 0)
                        {
                            ds.saveDelegationChanges( delegation, empId);
                            TempData["toastmessage"] = "Successfully edited delegation details";
                            return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                        }
                        else
                        {
                            
                            if (case3.Count != 0)
                            {

                                foreach (var item in case3)
                                {
                                    List<string> msg = ds.CaptureErrorMsg(item);
                                    msglist.Add(msg[0] + " is deleagated from " + msg[1] + " to" + msg[2]);
                                }
                                ViewData["list"] = msglist;
                                return View("EditDelegation");
                            }

                            else if (case4.Count != 0)
                            {

                                foreach (var item in case4)
                                {
                                    List<string> msg = ds.CaptureErrorMsg(item);
                                    msglist.Add(msg[0] + " is deleagated from " + msg[1] + " to" + msg[2]);
                                }
                                ViewData["list"] = msglist;
                                return View("EditDelegation");
                            }

                            else if (case1.Count != 0)
                            {
                                int flag = 0;
                                foreach (var item in case1)
                                {
                                    if (item.StartDate > delegation.EndDate)
                                    {
                                        flag++;
                                    }
                                    else
                                    {
                                        List<string> msg = ds.CaptureErrorMsg(item);
                                        msglist.Add(msg[0] + " is deleagated from " + msg[1] + " to" + msg[2]);
                                    }

                                }
                                if (flag == case1.Count && case2.Count == 0)
                                {
                                    ds.saveDelegationChanges(delegation, empId);
                                    TempData["toastmessage"] = "Successfully edited delegation details";
                                    return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                                }
                                else if (flag == case1.Count && case2.Count != 0)
                                {
                                    int flag1 = 0;
                                    foreach (var item in case2)
                                    {
                                        if (item.EndDate < delegation.StartDate)
                                        {
                                            flag1++;
                                        }
                                        else
                                        {
                                            List<string> msg = ds.CaptureErrorMsg(item);
                                            msglist.Add(msg[0] + " is  deleagated from " + msg[1] + " to" + msg[2]);
                                        }

                                    }
                                    if (flag1 == case2.Count)
                                    {
                                        ds.saveDelegationChanges(delegation, empId);
                                        TempData["toastmessage"] = "Successfully edited delegation details";
                                        return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                                    }
                                    else
                                    {
                                        ViewData["list"] = msglist;
                                        return View("EditDelegation");
                                    }


                                }
                                else
                                {
                                    ViewData["list"] = msglist;
                                    return View("EditDelegation");
                                }

                            }

                            else if (case2.Count != 0)
                            {
                                int flag = 0;
                                foreach (var item in case2)
                                {
                                    if (item.EndDate < delegation.StartDate)
                                    {
                                        flag++;
                                    }
                                    else
                                    {
                                        List<string> msg = ds.CaptureErrorMsg(item);
                                        msglist.Add(msg[0] + " is  deleagated from " + msg[1] + " to" + msg[2]);
                                    }

                                }
                                if (flag == case2.Count)
                                {
                                    ds.saveDelegationChanges(delegation, empId);
                                    TempData["toastmessage"] = "Successfully edited delegation details";
                                    return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
                                }
                                else
                                {
                                    ViewData["list"] = msglist;
                                    return View("EditDelegation");
                                }

                            }

                        }
                    }
                }
            }
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.DeptId == deptId), "EmployeeId", "EmployeeName", delegation.EmployeeId);
            return View(delegation);
        }





        // GET: Delegations/Delete/5
        public ActionResult CancelDelegation(int? id,string sessionId)
        {
            int empId = (int)Session["empId"];
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            if (Sessions.IsValidSession(sessionId))
            {
                ViewData["sessionId"] = sessionId;
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                Delegation delegation = db.Delegations.Include(d=>d.Employee).Where(d=>d.DelegationId==id).FirstOrDefault();
                if (delegation == null)
                {
                    return HttpNotFound();
                }
                return View(delegation);
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
        }

        // POST: Delegations/Delete/5
        [HttpPost, ActionName("cancelDelegation")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id,string sessionId)
        {
            ViewData["sessionId"] = sessionId;
            int empId = (int)Session["empId"];
            ViewData["Role"] = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Role).SingleOrDefault();
            var deptId = db.Employees.Where(r => r.EmployeeId == empId).Select(r => r.Department.DeptId).SingleOrDefault();
            Delegation delegation = db.Delegations.Find(id);
            Employee emp = db.Employees.Find(delegation.EmployeeId);
            var delegated = db.Delegations.Include(d => d.Employee).Where(d => d.DelegationId != id&&d.Employee.DeptId==deptId).ToList();
            var isDelegatedInFuture = true;
            if (!(delegation.EndDate < DateTime.Today)&&emp.DeptId==deptId)
            {
                foreach (var item in delegated)
                {
                    if (item.EmployeeId == delegation.EmployeeId && item.EndDate < DateTime.Today)
                    {
                        isDelegatedInFuture = false;
                    }
                    else
                    {
                        isDelegatedInFuture = true;
                        break;
                    }
                }
                if (isDelegatedInFuture == true)
                {
                    emp.Isdelegateded = "Y";
                }
                else
                {
                    emp.Isdelegateded = "N";
                }

                db.Delegations.Remove(delegation);
                db.SaveChanges();
                EmailService.SendNotification(delegation.EmployeeId, "Delegation Cancellation Reg.", "Delegation("+delegation.StartDate+"-"+delegation.EndDate+" is cancelled hereafter");
            }
            TempData["toastmessage"] = "Delegation Cancelled Successfully";
            return RedirectToAction("ViewDelegation", new { sessionId = sessionId });
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
