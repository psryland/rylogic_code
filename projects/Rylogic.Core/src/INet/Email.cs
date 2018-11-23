using System;
using System.Collections.Generic;
using System.Text;

namespace Rylogic.INet
{
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Net;
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Net.Mail;
	using System.Threading.Tasks;

	[TestFixture]
	public class TestEmail
	{
		/// <summary>How to send email the standard way</summary>
		public void SendEmail()
		{
			var from = new MailAddress("from@gmail.com", "From Name");
			var to = new MailAddress("to@yahoo.com", "To Name");
			var smtp = new SmtpClient
			{
				Host = "smtp.gmail.com", // "smtp.sendgrid.net", "smtp.mail.yahoo.com" , "smtp.live.com" (hotmail)
				Port = 587,              //  587               ,  587                  , 25
				Credentials = new NetworkCredential(from.Address, "from_password"),
				EnableSsl = true,
				DeliveryMethod = SmtpDeliveryMethod.Network,
				Timeout = 20000,
			};
			var msg = new MailMessage(from, to)
			{
				Subject = "Is this email",
				Body = "Nah...",
				IsBodyHtml = false,
			};

			using (smtp)
			using (msg)
				smtp.Send(msg);
		}

		public async Task SendEmailViaRPC()
		{
			// SendGrid RESTAPI
			// curl -i --request POST \
			// --url https://api.sendgrid.com/v3/mail/send \
			// --header 'Authorization: Bearer YOUR_API_KEY_HERE' \
			// --header 'Content-Type: application/json' \
			// --data '{"personalizations": [{"to": [{"email": "recipient@example.com"}]}],"from": {"email": "sendeexampexample@example.com"},"subject": "Hello, World!","content": [{"type": "text/plain", "value": "Howdy!"}]}'
			var http = new HttpClient();

			// Example uses the SendGrid API key/auth token for the RexBionics RST crash reporting
			// var api_key = "rE4Bg3AZRrOx7i8BrZgypA";
			// var auth_string = "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";
			http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "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");

			// Use 'JsonConvert.SerializeObject(body)'
			//  or 'System.Web.Extensions.JavaScriptSerializer().Serialize'
			var body = new
			{
				personalizations = new[]
				{
					new
					{
						to = new[]
						{
							new { email = "psryland@gmail.com" },
						},
					},
				},
				from = new
				{
					email = "sendeexampexample@example.com"
				},
				subject = "Hello, World!",
				content = new[]
				{
					new
					{
						type = "text/plain",
						value = "Howdy!",
					},
				}
			}.ToString();
			var uri = "https://api.sendgrid.com/v3/mail/send";
			var content = new StringContent(body, Encoding.UTF8, "application/json");
			using (var res = await http.PostAsync(uri, content))
			{
				if (!res.IsSuccessStatusCode)
					throw new Exception(res.ReasonPhrase);

				return; // Success
			};
		}

	}
}
#endif
