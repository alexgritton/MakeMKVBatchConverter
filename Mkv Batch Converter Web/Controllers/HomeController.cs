using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Mkv_Batch_Converter_Web.Models;

namespace Mkv_Batch_Converter_Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Contact(EmailModel emailModel)
        {
            if (emailModel == null) return View();
            if (!ModelState.IsValid)
            {
                return View(emailModel);
            }
            try
            {
                MailMessage mail = new MailMessage("makemkvbatch@gmail.com", "alexgritton@gmail.com");//FROM, TO
                SmtpClient client = new SmtpClient();
                client.Port = 587;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Timeout = 5000;
                client.Credentials = new NetworkCredential("makemkvbatch@gmail.com", "MakeMKVBatch123!@#");
                mail.Subject = "MakeMKV Batch Converter Comment";
                mail.Body = string.Format("Name: {0}\nEmail:{1}\nComments:{2}", emailModel.Name, emailModel.Email,
                    emailModel.Comment);
                client.Send(mail);
                return RedirectToAction("Email");
            }
            catch (Exception ex)
            {
                return View(emailModel);
            }
        }

        public ActionResult Email()
        {
            return View();
        }
    }
}