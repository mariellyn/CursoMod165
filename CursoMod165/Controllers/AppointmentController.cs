﻿using CursoMod165.Data;
using CursoMod165.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using NToastNotify;
using System.Text;
using static CursoMod165.CursoMod165Constants;

namespace CursoMod165.Controllers
{
    [Authorize(Policy = POLICIES.APP_POLICY.NAME)]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification; // Singleton
        private readonly IHtmlLocalizer<Resource> _sharedLocalizer;
        private readonly IStringLocalizer<Resource> _localizer;
        private readonly IEmailSender _emailSender;


        public AppointmentController(ApplicationDbContext context,
            IToastNotification toastNotification,
            IHtmlLocalizer<Resource> sharedLocalizer,
            IStringLocalizer<Resource> localizer,
            IEmailSender emailSender)
        {
            _context = context;
            _toastNotification = toastNotification;
            _sharedLocalizer = sharedLocalizer;
            _localizer = localizer;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            IEnumerable<Appointment> appointments = _context.Appointments
                                                            .Include(appointment => appointment.Staff.StaffRole)
                                                            .Include(appointment => appointment.Customer)
                                                            .ToList();
            
            return View(appointments);
        }



        public IActionResult TomorrowsAppointments()
        {
            var tomorrowsAppointments = _context.Appointments
                                                .Include(appointment => appointment.Staff.StaffRole)
                                                .Include(appointment => appointment.Customer)
                                                .Where(appointment => appointment.Date ==  DateTime.Today.AddDays(1))
                                                .ToList();
            return View(tomorrowsAppointments);
        }

        public IActionResult NextWeekAppointments()
        {
            DateTime startDate, endDate;
            GetNextWeeksDates(out startDate, out endDate);

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            List<Appointment> nextWeekAppointments = GetAppointmentsBetweenDates(startDate, endDate);
            return View(nextWeekAppointments);
        }

        private List<Appointment> GetAppointmentsBetweenDates(DateTime startDate, DateTime endDate)
        {
            return _context.Appointments
                            .Include(appointment => appointment.Staff.StaffRole)
                            .Include(appointment => appointment.Customer)
                            .Where(appointment => appointment.Date >= startDate && appointment.Date <= endDate)
                            .ToList();
        }

        private static void GetNextWeeksDates(out DateTime startDate, out DateTime endDate)
        {
            int x = 1;
            if (DateTime.Today.DayOfWeek != DayOfWeek.Sunday)
            {
                x = 8 - (int)DateTime.Today.DayOfWeek;
            }

            startDate = DateTime.Today.AddDays(x);
            endDate = DateTime.Today.AddDays(x + 4);
        }

        public IActionResult EmailReminderNextWeeksAppointments()
        {
            // Obter data de início e fim da próxima semana
            DateTime startDate, endDate;
            GetNextWeeksDates(out startDate, out endDate);


            // Obter as consultas da próxima semana
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            List<Appointment> nextWeekAppointments = GetAppointmentsBetweenDates(startDate, endDate);


            // Obter a língua e o respectivo template de e-mail
            var culture = Thread.CurrentThread.CurrentUICulture;

            string template = System.IO.File.ReadAllText(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "EmailTemplates",
                    $"next_week_appointment.{culture.Name}.html"
                )
            );

            // Adaptar o template a cada consulta e enviar o e-mail
            foreach(var appointment in nextWeekAppointments)
            {
                StringBuilder htmlBody = new StringBuilder(template);
                htmlBody.Replace("##CUSTOMER_NAME##", appointment.Customer.Name);
                htmlBody.Replace("##APPOINTMENT_DATE##", appointment.Date.ToShortDateString());
                htmlBody.Replace("##APPOINTMENT_TIME##", appointment.Time.ToShortTimeString());
                htmlBody.Replace("##STAFF_ROLE##", appointment.Staff.StaffRole.Name);
                htmlBody.Replace("##STAFF_NAME##", appointment.Staff.Name);


                _emailSender.SendEmailAsync(appointment.Customer.Email, "Reminder of Scheduled Appointment",
                    htmlBody.ToString());


            }
            
            _toastNotification.AddSuccessToastMessage($"{nextWeekAppointments.Count} Emails successfully sent.");


            return RedirectToAction(nameof(NextWeekAppointments));
        }






        [HttpGet]
        public IActionResult Create()
        {
            this.SetupAppointments();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                _context.Appointments.Add(appointment);
                _context.SaveChanges();

                // Toastr.SuccessMessage
                _toastNotification.AddSuccessToastMessage("Appointment successfully created.");


                
                Customer? customer = _context.Customers.Find(appointment.CustomerID);
                Staff? staff = _context.Staffs
                                        .Include(s => s.StaffRole)
                                        .Where(s => s.ID == appointment.StaffID)
                                        .Single();
                
                if (customer == null || staff == null)
                {
                    return NotFound();
                }

                var culture = Thread.CurrentThread.CurrentUICulture;

                string template = System.IO.File.ReadAllText(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "EmailTemplates",
                        $"create_appointment.{culture.Name}.html"
                    )
                );


                StringBuilder htmlBody = new StringBuilder(template);
                htmlBody.Replace("##CUSTOMER_NAME##", customer.Name);
                htmlBody.Replace("##APPOINTMENT_DATE##", appointment.Date.ToShortDateString());
                htmlBody.Replace("##APPOINTMENT_TIME##", appointment.Time.ToShortTimeString());
                htmlBody.Replace("##STAFF_ROLE##", staff.StaffRole.Name);
                htmlBody.Replace("##STAFF_NAME##", staff.Name);

               


                
                _emailSender.SendEmailAsync(customer.Email, "Appointment Scheduled",
                    htmlBody.ToString());

                return RedirectToAction(nameof(Index));
            }

            // Toastr.ErrorMessage
            _toastNotification.AddErrorToastMessage("Check the form again!",
                new ToastrOptions { 
                    Title = "Appointment Creation Error",
                    TapToDismiss = true,
                    TimeOut = 0
                });
            this.SetupAppointments();
            return View(appointment);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            Appointment? appointment = _context.Appointments.Find(id);

            if (appointment == null)
            {
                return RedirectToAction(nameof(Index));
            }

            this.SetupAppointments();
            return View(appointment);
        }

        [HttpPost]
        public IActionResult Edit(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                _context.Appointments.Update(appointment);
                _context.SaveChanges();


                string message = 
                    string.Format(_sharedLocalizer["<b>Appointment # {0}</b> successfully edited!"].Value,
                                  appointment.Number);


                message += "<br />" + string.Format(_sharedLocalizer["Date: <b>{0}</b> at <b>{1}</b>"].Value,
                                  appointment.Date.ToShortDateString(),
                                  appointment.Time.ToShortTimeString());

                _toastNotification.AddSuccessToastMessage(message, 
                    new ToastrOptions { Title = _sharedLocalizer["Success"].Value,
                        TimeOut = 0,
                        TapToDismiss = true
                    });

                

                _emailSender.SendEmailAsync("diogothesilva@gmail.com",
                    "Edit Appointment", "Edited successfully");


                return RedirectToAction(nameof(Index));
            }

            this.SetupAppointments();
            return View(appointment);
        }


        private void SetupAppointments()
        {
            ViewBag.CustomerList = new SelectList(_context.Customers, "ID", "Name");

            var staffList = _context.Staffs
                                    .Include(s => s.StaffRole)
                                    .Where(s => s.StaffRole.CanDoAppointments == true)
                                    .Select(s => new { 
                                                        ID = s.ID,
                                                        Name = $"{s.Name} [{s.StaffRole.Name}]" 
                                                     });

            ViewBag.StaffList = new SelectList(staffList, "ID", "Name");
        }
    }
}
