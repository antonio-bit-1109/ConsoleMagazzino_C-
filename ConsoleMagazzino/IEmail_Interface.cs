using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleMagazzino
{
    internal interface IEmail_Interface
    {
        void SendEmail_classEmail(string destinatario, string oggettoEMail, string testoEMail);
        void CheckIndirizziEmailSent();
    }
}
