using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

namespace CIAPI.Helpers
{
    public static class EMailManager
    {
        public static void SendSimpleMail(string nombreDestinatario, string correoDestinatario,string asunto, string cuerpoText, bool isHTML=false)
        {
            MimeMessage message = new MimeMessage();

            MailboxAddress from = new MailboxAddress(MySettingsBase.CORREO_NOMBRE_ORIGEN,
            MySettingsBase.CORREO_SEND_MAIL);
            message.From.Add(from);

            MailboxAddress to = new MailboxAddress(nombreDestinatario,
            correoDestinatario);
            message.To.Add(to);

            message.Subject = asunto;


            //Cuerpo
            BodyBuilder bodyBuilder = new BodyBuilder();
            if(isHTML)
                bodyBuilder.HtmlBody = cuerpoText;
            else
                bodyBuilder.TextBody = cuerpoText;

            message.Body = bodyBuilder.ToMessageBody();

            //Asi se podria agregar attachments
            //bodyBuilder.Attachments.Add(env.WebRootPath + "\\file.png");


            //Configurar SMTP


            SmtpClient client = new SmtpClient();

            client.CheckCertificateRevocation = false;
            client.Connect(MySettingsBase.SERVIDOR_SMTP, MySettingsBase.PUERTO_SMTP,MailKit.Security.SecureSocketOptions.Auto);

            if(!String.IsNullOrWhiteSpace(MySettingsBase.PWD_CORREO))  client.Authenticate(MySettingsBase.CORREO_SEND_MAIL, MySettingsBase.PWD_CORREO);


            

            client.Send(message);
            client.Disconnect(true);
            client.Dispose();
        }
    }
}
