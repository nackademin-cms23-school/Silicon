using System;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using EmailProvider.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmailProvider.Functions;

public class EmailSender(ILogger<EmailSender> logger, EmailClient emailClient)
{
    private readonly ILogger<EmailSender> _logger = logger;
    private readonly EmailClient _emailClient = emailClient;

    [Function(nameof(EmailSender))]
    public async Task Run([ServiceBusTrigger("email_request", Connection = "SeriviceBus")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
    {
        try
        {
            var request = UnpackEmailRequest(message);
            if (request != null && !string.IsNullOrEmpty(request.To))
            {
                var result = _emailClient.Send(
                    Azure.WaitUntil.Completed,
                    senderAddress: Environment.GetEnvironmentVariable("SenderAddress"),
                    recipientAddress: request.To,
                    subject: request.Subject,
                    htmlContent: request.Body,
                    plainTextContent: request.PlainText
                    );

                if(result.HasCompleted)
                {
                    await messageActions.CompleteMessageAsync(message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error :: Run :: {ex.Message} ");
        }
    }


    public EmailRequest UnpackEmailRequest(ServiceBusReceivedMessage message)
    {
        try
        {
            var request = JsonConvert.DeserializeObject<EmailRequest>(message.Body.ToString());
            if (request != null)
            {
                return request;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error :: UnpackEmailRequest :: {ex.Message} ");
        }
        return null!;
    }
}
