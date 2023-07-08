using JoeScan.LogScanner.Core.Interfaces;

namespace JoeScan.LogScanner.Core.Models;

public class LogModelSender : ILogModelSender
{
    public Task SendAsync(LogModelResult result, CancellationToken cancellationToken = default)
    {
        
        return Task.CompletedTask;
    }
}
