using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data;
namespace DoctorAppointment.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            return View();
        }


     
        /// <summary>
        /// To view user login page
        /// </summary>
        /// <returns></returns>
        public ActionResult UserLogin()
        {
            return View();
        }


        /// <summary>
        /// To validate the given email id
        /// </summary>
        /// <param name="emailId"></param>
        /// <returns></returns>
        [HttpPost]
        public string ValidateEmailId(string emailId)
        {
            Appoinment_Entities appoinment_Entities_db = new Appoinment_Entities();
            var emailStatus = appoinment_Entities_db.user_Info.Where(m => m.email_Address == emailId).FirstOrDefault();
            if (emailStatus != null)
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }


        /// <summary>
        /// To validate the given mobile number
        /// </summary>
        /// <param name="mobileNumber"></param>
        /// <returns></returns>
        [HttpPost]
        public string ValidateMobileNumber(string mobileNumber)
        {
            Appoinment_Entities appoinment_Entities_db = new Appoinment_Entities();
            var mobile = appoinment_Entities_db.user_Info.Where(m => m.mobile_Number  == mobileNumber).FirstOrDefault();
            if (mobile != null)
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }



        /// <summary>
        /// To validate the user credentials
        /// </summary>
        /// <param name="userLogin"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserLogin(userLogin userLogin)
        {
            if (ModelState.IsValid) // this is check validity
            {
                using (Appoinment_Entities appoinment_Entities_db = new Appoinment_Entities())
                {
                    var userCredentials = appoinment_Entities_db.userLogins.Where(a => a.UserName.Equals(userLogin.UserName) && a.Password.Equals(userLogin.Password)).FirstOrDefault();
                    if (userCredentials != null)
                    {
                        Session["LogedUserID"] = userCredentials.UserID.ToString();
                        Session["LogedUserFullname"] = userCredentials.FullName.ToString();
                        Session["LoggedRoleID"] = userCredentials.RoleID.ToString();
                        if (Convert.ToInt32(Session["LoggedRoleID"].ToString()) == 1)
                        {
                            return RedirectToAction("CreateUser", userLogin.UserID);
                        }
                        else if (Convert.ToInt32(Session["LoggedRoleID"].ToString()) == 2)
                        {
                            return RedirectToAction("ShowAppointment");
                        }
                        else if (Convert.ToInt32(Session["LoggedRoleID"].ToString()) == 3)
                        {
                            return RedirectToAction("CreateAppointment");
                        }

                    }
                    return View("Index");
                }
            }

            return View(userLogin);
        }




        /// <summary>
        /// To save the user details
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CreateUser(user_Info userInfo)
        {
            if (ModelState.IsValid) // this is check validity
            {
                using (Appoinment_Entities appoinment_Entities_db = new Appoinment_Entities())
                {
                    userLogin userLogin = new userLogin();
                    doctorDetail doctorDetails = new doctorDetail();
                    userRole userRole = new userRole();
                    appoinment_Entities_db.user_Info.Add(userInfo);
                    appoinment_Entities_db.SaveChanges();
                    userLogin.FullName = userInfo.first_Name + userInfo.last_Name;
                    userLogin.RoleID = userInfo.role_Id;

                    var userName = appoinment_Entities_db.userRoles.Where(
                  i => i.role_Id == userLogin.RoleID).SingleOrDefault().role_Name;
                    userLogin.UserID = userInfo.user_Id;
                    userLogin.UserName = userInfo.email_Address;
                    userLogin.Password = userInfo.first_Name + "@123";
                    appoinment_Entities_db.userLogins.Add(userLogin);
                    appoinment_Entities_db.SaveChanges();

                    if (userInfo.role_Id == 2)  
                    {
                        doctorDetails.doctor_Id = userInfo.user_Id;
                        doctorDetails.startTime = userInfo.doctorDetail.startTime;
                        doctorDetails.endTime = userInfo.doctorDetail.endTime;
                        appoinment_Entities_db.doctorDetails.Add(doctorDetails);
                        appoinment_Entities_db.SaveChanges();
                    }
                }
                ViewBag.Message = "User Created Succesfully";
                return View("DisplayMessage");
            }
            else
            {
                return RedirectToAction("CreateUser","Home");
            }
        }



        /// <summary>
        /// To input the user details
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult CreateUser()
        {
            var userInfo = new user_Info();

            if (ModelState.IsValid) // this is check validity
            {
                
                using (var appoinment_Entities_db = new Appoinment_Entities())
                {
                    userInfo.CreateRole = appoinment_Entities_db.userRoles.ToList().Select(x => new SelectListItem
                    {
                        Value = x.role_Id.ToString(),
                        Text = x.role_Name
                    });
                }
                return View(userInfo);
            }
            else
                return View(userInfo);
        }


        /// <summary>
        /// To create the doctor appointment
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult CreateAppointment()
        {

            Appoinment_Entities appoinment_Entities_db = new Appoinment_Entities();
            List<doctorDetail> doctorDetailsList = new List<doctorDetail>();
            //appointment app = new appointment();

            userLogin login = new userLogin();
            var drafts = appoinment_Entities_db.userLogins.Where(x => x.RoleID == 2).ToList();
            if (drafts.Count != 0)
            {
                foreach (var doc in drafts)
                {
                    doctorDetail docdetail = new doctorDetail();
                    var docId = appoinment_Entities_db.doctorDetails.Where(
                        i => i.doctor_Id == doc.UserID).Single();
                    var userName = appoinment_Entities_db.userLogins.Where(
                        i => i.UserID == docId.doctor_Id).Single().FullName;
                    docdetail.startTime = docId.startTime;
                    docdetail.doctor_Id = docId.doctor_Id;
                    docdetail.endTime = docId.endTime;
                    docdetail.Doctor_Name = userName;
                    List<string> timeIntervals = new List<string>();
                    TimeSpan? approvedSlots = new TimeSpan();
                    //var approvedSlots;
                    var slots = appoinment_Entities_db.appointmentInfoes.Where(x => x.doctor_Id == docdetail.doctor_Id && x.appointment_Status == true).ToList();


                    DateTime date = Convert.ToDateTime(docId.startTime);
                    DateTime endDate = Convert.ToDateTime(docId.endTime);
                    while (date < endDate)
                    {
                        timeIntervals.Add(date.ToString("hh:mm:ss"));
                        date = date.AddMinutes(15);
                    }
                    docdetail.Interval = timeIntervals.Select(x => new SelectListItem() { Value = x, Text = x }).ToList();
                    foreach (var approvedTime in slots)
                    {
                        docdetail.Interval.RemoveAll(c => c.Value == Convert.ToString(approvedTime.appointment_Time));
                    }


                    doctorDetailsList.Add(docdetail);
                }
            }

            return View(doctorDetailsList);

        }


        /// <summary>
        /// To save the appointment details
        /// </summary>
        /// <param name="doctorId"></param>
        /// <param name="appointmentTime"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CreateAppointment(int doctorId, string appointmentTime)
        {
            Appoinment_Entities appoinment_Entities_db = new Appoinment_Entities();
            appointmentInfo appointment = new appointmentInfo();
            appointment.patient_Id = Convert.ToInt32(Session["LogedUserID"]);
            var patientAppointment= appoinment_Entities_db.appointmentInfoes.Where(
                       i => i.patient_Id  == appointment.patient_Id && i.appointment_Status==true).ToList();
            if (patientAppointment.Count == 0)
            {
                userLogin login = new userLogin();
                //appointment.appointment_Id = 1;
                //appointment.appointment_Id = appoinment_Entities_db.appointmentInfoes.Select(c => c.appointment_Id).Count() + 1;
                appointment.doctor_Id = doctorId;

                var doctorName = appoinment_Entities_db.userLogins.Where(
                       i => i.UserID == doctorId).Single().FullName;

                appointment.doctor_Name = doctorName;

                appointment.patient_Name = appoinment_Entities_db.userLogins.Where(
                           i => i.UserID == appointment.patient_Id).Single().FullName;
                DateTime date = DateTime.Now;
                var shortDate = date.Date;
                appointment.appointment_Date = shortDate;
                appointment.appointment_Time = TimeSpan.Parse(appointmentTime);
                appointment.appointment_Status = false;
                appoinment_Entities_db.appointmentInfoes.Add(appointment);
                appoinment_Entities_db.SaveChanges();
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json("Fail", JsonRequestBehavior.AllowGet);
            }
        }
       


        /// <summary>
        /// To view the appointments
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult ShowAppointment()
        {
            Appoinment_Entities appoinment_Entities_db = new Appoinment_Entities();
            appointmentInfo appointments = new appointmentInfo();
            appointments.doctor_Id= Convert.ToInt32(Session["LogedUserID"]);
            var appointment = appoinment_Entities_db.appointmentInfoes.Where(x => x.doctor_Id == appointments.doctor_Id && x.appointment_Status==false).ToList();
            if (appointment.Count != 0)
            {
                return View(appointment);
            }
            else
                return View();
        }



        /// <summary>
        /// To save the appointment status
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ShowAppointment(int? patientId, string status)
        {
            Appoinment_Entities appoinment_Entities_db = new Appoinment_Entities();
            appointmentInfo appointmentInfo = new appointmentInfo();
            appointmentInfo.doctor_Id = Convert.ToInt32(Session["LogedUserID"]);
            appointmentInfo = appoinment_Entities_db.appointmentInfoes.Where(x => x.patient_Id == patientId && x.doctor_Id == appointmentInfo.doctor_Id).FirstOrDefault();
            if (appointmentInfo != null)
            { 
                if (status == "Confirm")
                    appointmentInfo.appointment_Status = true;
                else
                    appointmentInfo.appointment_Status = false;
                appoinment_Entities_db.appointmentInfoes.Add(appointmentInfo);
                appoinment_Entities_db.Entry(appointmentInfo).State = System.Data.Entity.EntityState.Modified;
                appoinment_Entities_db.SaveChanges();
                return Json("Success", JsonRequestBehavior.AllowGet);
            }
            return Json("Fail", JsonRequestBehavior.AllowGet);
        }

    }
}
   