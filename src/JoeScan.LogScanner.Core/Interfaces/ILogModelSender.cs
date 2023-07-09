using JoeScan.LogScanner.Core.Models;

namespace JoeScan.LogScanner.Core.Interfaces;

public interface ILogModelSender
{
    Task SendAsync(LogModelResult result, CancellationToken cancellationToken = default);
    void Start();
    void Stop();
}
