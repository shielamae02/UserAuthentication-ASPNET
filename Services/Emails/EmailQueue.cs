using System.Collections.Concurrent;
namespace UserAuthentication_ASPNET.Services.Emails;

public class EmailQueue
{
    private readonly ConcurrentQueue<(IEnumerable<string> emails, string subject, string content)> _emailQueue = new();


}