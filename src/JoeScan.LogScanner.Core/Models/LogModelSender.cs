using FlatSharp;
using JoeScan.LogScanner.Core.Extensions;
using JoeScan.LogScanner.Core.Interfaces;
using JoeScan.LogScanner.Core.Schema;
using MathNet.Numerics.Statistics;
using Microsoft.Identity.Client;
using NLog;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Logger = NLog.Logger;


namespace JoeScan.LogScanner.Core.Models;

public class LogModelSender : ILogModelSender, IDisposable
{
    private ILogger logger;
    private const int ModelSenderPort = 10020;
    private TcpListener? server;
    private CancellationTokenSource? cancellationTokenSource;
    private Thread? listenerThread;
    private readonly List<LogModelResultTSenderClient> clients = new();

    public LogModelSender(ILogger logger)
    {
        this.logger = logger;
        this.logger.Debug("Created LogModelSender");
    }

    public void Start()
    {
        logger.Debug("Starting LogModelSender");
        if (cancellationTokenSource == null)
        {
            cancellationTokenSource = new CancellationTokenSource();
            listenerThread = new Thread(() => StartListener(cancellationTokenSource.Token));
            listenerThread.Start();
        }
        else
        {
            logger.Debug("LogModelSender already started");
        }
    }

    public void Stop()
    {
        logger.Debug("Stopping LogModelSender");
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            listenerThread!.Join();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            logger.Debug("LogModelSender stopped");
        }
        else
        {
            logger.Debug("LogModelSender already stopped");
        }
    }

    private async void StartListener(object token)
    {
        var cancellationToken = (CancellationToken)token;
        server = new TcpListener(IPAddress.Any, ModelSenderPort);
        server.Start();
        logger.Debug($"LogModelSender listening on port {ModelSenderPort} for clients.");

        try
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.Debug("LogModelSender cancellation requested");
                    break;
                }

                logger.Debug("Waiting for a connection...");
                var client = await server!.AcceptTcpClientAsync(cancellationToken);

                logger.Debug("Connected!");
                var t = new LogModelResultTSenderClient(client, $"Client #{clients.Count + 1}");
                clients.Add(t);
            }
        }
        catch (OperationCanceledException)
        {
            // nothing
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error in LogModelSender");
        }
        finally
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
        }

        logger.Debug("LogModelSender listener thread exiting");
    }
    #region ILogModelSender Implementation

    public async Task SendAsync(LogModelResult result, CancellationToken cancellationToken = default)
    {
        if (clients.Count == 0)
        {
            logger.Debug("No clients connected, not sending");
            return;
        }
        var logModelResultT = result.ToLogModelResultT();
        int maxBytesNeeded = LogModelResultT.Serializer.GetMaxSize(logModelResultT);
        byte[] buffer = new byte[maxBytesNeeded];
        int bytesWritten = LogModelResultT.Serializer.Write(buffer, logModelResultT);
        // TODO: use Task.WaitAll() to send to all clients in parallel
        foreach (var logModelResultTSenderClient in clients)
        {
            await logModelResultTSenderClient.Send(buffer,bytesWritten);
        }
    }

    #endregion

    public void Dispose()
    {
        Stop();
    }
}

internal class LogModelResultTSenderClient : IDisposable
{
    private string Name { get; }
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private readonly TcpClient client;
    private readonly NetworkStream stream;

    public LogModelResultTSenderClient(TcpClient client, string name)
    {
        Name = name;
        this.client = client;
        logger.Debug($"Created LogModelResultTSenderClient {Name}");
        stream = client.GetStream();
    }
    
    public void Dispose()
    {
        stream.Close();
        client.Close();
    }

    public async Task Send(byte[] data, int length)
    { 
        logger.Trace($"Sending {length} bytes to client.");
        await stream.WriteAsync(BitConverter.GetBytes(length), 0, sizeof(int));
        await stream.WriteAsync(data, 0, length);
    }
}
