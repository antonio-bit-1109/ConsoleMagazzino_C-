using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConsoleMagazzino
{
    internal class InvioEmail : IEmail_Interface
    {
        private string Destinatario { get; set; }
        private string Mittente { get; set; } = "antoniorizzuti767@gmail.com";
        private string PswMitt { get; set; } = "";
        private string OggettoEmail { get; set; }
        private string TestoEmail { get; set; }

        // indirizzi a cui in precedenza è stata mandata un email con successo.
        private List<string> Indirizzi = new List<string>();


        public InvioEmail()
        {
            PswMitt = Environment.GetEnvironmentVariable("PASSWORD_EMAIL_CONSOLE_APP");

            if (PswMitt == "")
            {
                throw new Exception("Impossibile recuperare password dalle varibili d'ambiente.");
            }
        }

        public void SendEmail_classEmail (string destinatario , string oggettoEMail , string testoEMail)
        {
            if (string.IsNullOrEmpty(destinatario) || string.IsNullOrEmpty(oggettoEMail) || string.IsNullOrEmpty(testoEMail))
            {
                throw new ArgumentNullException("Uno dei necessari all'invio della mail è vuoto.");
            }

            this.Destinatario = destinatario;
            this.OggettoEmail = oggettoEMail;
            this.TestoEmail = testoEMail;

            try
            {
                var fromAddress = new MailAddress(Mittente);
                var toAddress = new MailAddress(Destinatario);
                //string fromPassword = PswMitt;    
                string fromPassword = PswMitt;
                const string smtpServer = "smtp.gmail.com";
                const int smtpPort = 587;

                var smtp = new SmtpClient
                {
                    Host = smtpServer,
                    Port = smtpPort,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = oggettoEMail,
                    Body = testoEMail
                })
                {
                    smtp.Send(message);
                }
                Console.WriteLine("  ---   ----   ----   ---- ");
                Console.WriteLine("Email inviata con successo.");
                Console.WriteLine("---   ---  ----  ----  --- ");
                Indirizzi.Add(destinatario);

            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Errore nell'invio della mail: {ex.Message}");
            }
        }

        // metodo per controllare a quali indirizzi si è inviato con successo una mail.
        public void CheckIndirizziEmailSent()
        {
           try
           {
                if (Indirizzi.Count <= 0)
                {
                    throw new Exception("Non ci sono indirizzi a cui è stata inviata con successo una email.");
                }

                Console.WriteLine("Indirizzi a cui è stata inviata una mail con successo: ");

                Console.WriteLine("  ---   ----   ----   ---- ");
                foreach(var indirizzo in Indirizzi)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(indirizzo);
                    Console.ResetColor();
                }
                    Console.WriteLine("---   ---  ----  ----  --- ");
                

           } catch (Exception ex)
           {
                Console.WriteLine($"Errore nel controllo degli indirizzi a cui è stata inviata una mail con successo: {ex.Message}");
           }
        }
    }
}
