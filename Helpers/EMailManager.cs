using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CIAPI.Helpers
{
    public static class EMailManager
    {
        private static readonly HttpClient _http = new HttpClient();
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Si va a enviar a mas de una dirección separar con punto y coma.
        /// Usa ZeptoMail (HTTPS) si ZEPTO_API_KEY está configurado, sino SMTP.
        /// </summary>
        public static void SendSimpleMail(string nombreDestinatario, string correoDestinatario, string asunto, string cuerpoText, bool isHTML = false)
        {
            if (!string.IsNullOrWhiteSpace(MySettingsBase.ZEPTO_API_KEY))
            {
                SendViaZeptoMail(nombreDestinatario, correoDestinatario, asunto, cuerpoText, isHTML);
                return;
            }

            MimeMessage message = new MimeMessage();

            MailboxAddress from = new MailboxAddress(MySettingsBase.CORREO_NOMBRE_ORIGEN,
            MySettingsBase.CORREO_SEND_MAIL);
            message.From.Add(from);

            var listaDest = correoDestinatario.Split(';');
            int i = 0;
            foreach (var unDest in listaDest)
            {
                i++;
                MailboxAddress to = new MailboxAddress(nombreDestinatario + (i == 1 ? "" : i.ToString()),
                unDest);
                message.To.Add(to);
            }

            message.Subject = asunto;

            BodyBuilder bodyBuilder = new BodyBuilder();
            if (isHTML)
                bodyBuilder.HtmlBody = cuerpoText;
            else
                bodyBuilder.TextBody = cuerpoText;

            message.Body = bodyBuilder.ToMessageBody();

            using SmtpClient client = new SmtpClient();
            client.Timeout = 10000;
            client.CheckCertificateRevocation = false;
            client.Connect(MySettingsBase.SERVIDOR_SMTP, MySettingsBase.PUERTO_SMTP, MailKit.Security.SecureSocketOptions.Auto);

            if (!String.IsNullOrWhiteSpace(MySettingsBase.PWD_CORREO)) client.Authenticate(MySettingsBase.CORREO_SEND_MAIL, MySettingsBase.PWD_CORREO);

            client.Send(message);
        }

        private static void SendViaZeptoMail(string nombreDestinatario, string correoDestinatario, string asunto, string cuerpoText, bool isHTML)
        {
            var dests = correoDestinatario.Split(';');
            var toList = new object[dests.Length];
            for (int i = 0; i < dests.Length; i++)
                toList[i] = new { email_address = new { address = dests[i].Trim(), name = nombreDestinatario + (i == 0 ? "" : (i + 1).ToString()) } };

            var payload = new
            {
                from = new { address = MySettingsBase.CORREO_SEND_MAIL, name = MySettingsBase.CORREO_NOMBRE_ORIGEN },
                to = toList,
                subject = asunto,
                htmlbody = isHTML ? cuerpoText : null,
                textbody = isHTML ? null : cuerpoText
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.zeptomail.com/v1.1/email");
            request.Headers.Add("Authorization", MySettingsBase.ZEPTO_API_KEY);
            request.Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOpts), Encoding.UTF8, "application/json");

            var response = _http.Send(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new Exception($"ZeptoMail error {(int)response.StatusCode}: {body}");
            }
        }
    }
}
