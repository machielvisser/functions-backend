#r "Newtonsoft.Json"

using System;
using System.Net;
using Newtonsoft.Json;
using System.Net.Mail;
using Microsoft.WindowsAzure.Storage.Table; 
using System.Linq;

public class RecipientEnitity : TableEntity
{
    public string Recipient { get; set; }
}

public static async Task<object> Run(
    HttpRequestMessage req, 
    IQueryable<RecipientEnitity> recipients, 
    TraceWriter log)
{
    log.Info($"Webhook was triggered!");

    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);

    string appid = data.appid.ToString();
    var name = data.name.ToString();
    var message = data.message.ToString();
    var email = data.email.ToString();
    
    if (string.IsNullOrEmpty(appid) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(message) || string.IsNullOrEmpty(email))
        return req.CreateResponse(HttpStatusCode.BadRequest);

    var server = System.Environment.GetEnvironmentVariable("SMTP_SERVER", EnvironmentVariableTarget.Process);
    var from = System.Environment.GetEnvironmentVariable("SMTP_LOGIN", EnvironmentVariableTarget.Process);
    var password = System.Environment.GetEnvironmentVariable("SMTP_PASSWORD", EnvironmentVariableTarget.Process);
    
    var recipient = recipients.Where(r => r.RowKey == appid).Select(r => r.Recipient).FirstOrDefault();
    if (string.IsNullOrEmpty(recipient))
        return req.CreateResponse(HttpStatusCode.InternalServerError);
    log.Info($"Recipient found: {recipient}");
            
    MailMessage mail = new MailMessage(from, recipient);
    mail.Subject = $"A message from {name}";    
    mail.Body = message;
    mail.ReplyToList.Add(email);

    SmtpClient client = new SmtpClient();
    client.Port = 587;
    client.EnableSsl = true;
    client.DeliveryMethod = SmtpDeliveryMethod.Network;
    client.UseDefaultCredentials = false;
    client.Host = server;
    client.Credentials = new System.Net.NetworkCredential(from, password);

    try {
      client.Send(mail);
      log.Info("Email sent");
      return req.CreateResponse(HttpStatusCode.OK, new {
            status = true,
            message = string.Empty
        });
    }
    catch (Exception ex) {
      log.Info(ex.ToString());
      return req.CreateResponse(HttpStatusCode.InternalServerError, new {
            status = false,
            message = "Message has not been sent. Check Azure Function Logs for more information."
        });
    }
}