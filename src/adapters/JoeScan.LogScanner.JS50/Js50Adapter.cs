﻿using Autofac.Features.AttributeFilters;
using Config.Net;
using JoeScan.LogScanner.Core.Events;
using JoeScan.LogScanner.Core.Extensions;
using JoeScan.LogScanner.Core.Interfaces;
using JoeScan.LogScanner.Core.Models;
using JoeScan.Pinchot;
using NLog;
using System.Reflection;
using System.Threading.Tasks.Dataflow;
using Profile = JoeScan.LogScanner.Core.Models.Profile;

namespace JoeScan.LogScanner.Js50;

public class Js50Adapter : IScannerAdapter
{
    #region Private Fields

    private ScanSystem? scanSystem;
    private CancellationTokenSource? cancellationTokenSource;
    private Thread? scanThread;
    private bool isRunning;
    static readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
    private const int maxStartupTimeS = 10;

    #endregion

    #region Injected

    private readonly ILogger? logger;
    private IJs50AdapterConfig Config { get; }
    private readonly ScanSyncReceiverThread encoderUpdater;

    #endregion

    #region Lifecycle

    public Js50Adapter(ILogger logger)
    {
        Config = new ConfigurationBuilder<IJs50AdapterConfig>()
            .UseJsonFile("js50adapter.json")
            .Build();
        this.logger = logger;
        encoderUpdater = new ScanSyncReceiverThread(logger);
        logger.Debug($"Created Js50Adapter using JoeScan Pinchot API version {Pinchot.VersionInformation.Version}");
        logger.Debug($"Created Js50Adapter using js50Adapter {Assembly.GetExecutingAssembly()}");
        Units = UnitSystem.Millimeters;
        encoderUpdater.EventUpdateFrequencyMs = 100;
        encoderUpdater.ScanSyncUpdate += EncoderUpdaterOnScanSyncUpdate;
    }

    #endregion

    #region IScannerAdapter Implementation
    public string Name => $"JS-50";
    public UnitSystem Units { get; }
    public bool IsConfigured { get; private set; }


    // If we give a finite BoundedCapacity, the BufferBlock will discard Profiles 
    // i.e Post() will return false. -1 means unlimited buffering - hopefully the 
    // subsequent steps are fast enough not to make this a memory hog
    public BufferBlock<Profile> AvailableProfiles { get; } =
        new BufferBlock<Profile>(new DataflowBlockOptions() { BoundedCapacity = -1 });

    public void Configure()
    {
        try
        {
            encoderUpdater.Start();
            IsConfigured = true;
            logger!.Debug("Started ScanSyncReceiverThread.");
        }
        catch (Exception e)
        {
            logger!.Error($"Could not start ScanSyncReceiverThread: {e.Message}");
        }
    }

    public void Start()
    {
        logger!.Debug($"Trying to start {this.GetType().Name}.");
        if (!IsConfigured)
        {
            throw new ApplicationException(
                $"{this.GetType().Name} not configured. You need to call Configure() before calling Start().");
        }
        if (!IsRunning)
        {
            scanSystem = SetupScanSystem();
            if (scanSystem != null)
            {
                // kick off scan thread
                cancellationTokenSource = new CancellationTokenSource();
                ThreadStart threadMainStart = delegate { ScanLoop(cancellationTokenSource.Token); };
                scanThread = new Thread(threadMainStart) { Priority = ThreadPriority.Normal, IsBackground = true };
                scanThread.Start();
                if (!autoResetEvent.WaitOne(TimeSpan.FromSeconds(maxStartupTimeS)))
                {
                    string msg = "Timed out connecting.";
                    logger.Error(msg);
                    throw new ApplicationException(msg);
                }
                IsRunning = true;
            }
            else
            {
                string msg = "Could not create ScanSystem.";
                logger.Error(msg);
                throw new ApplicationException(msg);
            }
        }
        else
        {
            string msg = "Failed to Start: adapter already running.";
            logger.Error(msg);
            throw new ApplicationException(msg);
        }
    }



    public void Stop()
    {
        logger!.Debug($"Trying to stop {this.GetType().Name}.");
        if (!IsRunning)
        {
            return;
        }
        cancellationTokenSource!.Cancel();
        if (!scanThread!.Join(TimeSpan.FromSeconds(1)))
        {
            logger.Warn("ScanThread did not exit. Abandoning it.");
        }
        else
        {
            logger.Debug("Clean shutdown of scan thread successful.");
        }

        if (scanSystem != null)
        {
            scanSystem.Dispose();
            scanSystem = null;
        }
        scanThread = null;
        cancellationTokenSource = null;
    }

    public bool IsRunning
    {
        get => isRunning;
        set
        {
            if (value != isRunning)
            {
                isRunning = value;
                if (isRunning)
                {
                    OnScanningStarted();
                }
                else
                {
                    OnScanningStopped();
                }
            }
        }
    }

    public event EventHandler? ScanningStarted;
    public event EventHandler? ScanningStopped;
    public event EventHandler? ScanErrorEncountered;
    public event EventHandler<EncoderUpdateArgs>? EncoderUpdated;
    public bool IsReplay => false;
    public uint VersionMajor => 1;
    public uint VersionMinor => 0;
    public uint VersionPatch => 0;
    public Guid Id { get; } = Guid.Parse("{D1021E6A-7C7C-43F0-9444-303FDCAE2BFF}");

    #endregion

    private void ScanLoop(CancellationToken ct)
    {
        try
        {
            var timeOut = TimeSpan.FromSeconds(10);
            logger!.Debug($"Attempting to connect to scan heads with timeout of {timeOut} seconds.");
            var disconnectedHeads = scanSystem!.Connect(timeOut);
            if (disconnectedHeads == null)
            {
                throw new InvalidOperationException("ScanSystem.Connect() call timed out.");
            }
            if (disconnectedHeads.Any())
            {
                foreach (var scanHead in disconnectedHeads)
                {
                    logger.Error($"Scan head {scanHead.SerialNumber} did not respond to connection request.");
                }
                throw new InvalidOperationException($"Not starting scan thread due to connection error.");
            }
            logger.Debug($"All scan heads connected.");
            foreach (var scanHead in scanSystem.ScanHeads)
            {
                logger.Debug($"Active scan head: {scanHead.SerialNumber} ({scanHead.ID}) FW: {scanHead.Version}");
            }

            foreach (var scanHead in scanSystem.ScanHeads)
            {
                scanSystem.AddPhase();
                scanSystem.AddPhaseElement(scanHead.ID, Camera.CameraA);
                scanSystem.AddPhaseElement(scanHead.ID, Camera.CameraB);
            }


            uint minScanPeriodUs = scanSystem.GetMinScanPeriod();
            logger.Debug($"The system has a min scan period of {minScanPeriodUs}µs.");
            uint requestedScanPeriodUs = Config.ScanPeriodUs;

            if (minScanPeriodUs > requestedScanPeriodUs)
            {
                logger.Warn($"The requested scan period of {requestedScanPeriodUs}µs is too fast for the system. Using {minScanPeriodUs}µs instead.");
                requestedScanPeriodUs = minScanPeriodUs+10;
            }
            else
            {
                logger.Debug($"Using requested scan period of {requestedScanPeriodUs}μs.");
            }
            logger.Debug($"Requested DataFormat is {Config.DataFormat}");
            scanSystem.StartScanning(requestedScanPeriodUs, Config.DataFormat);

            int failedToPost = 0;
            // we seem to have connected and are scanning
            autoResetEvent.Set();
            while (!ct.IsCancellationRequested)
            {
                // post all profiles that are due
                ct.ThrowIfCancellationRequested();
                foreach (var scanHead in scanSystem.ScanHeads)
                {
                    var gotProfile = scanHead.TryTakeNextProfile(out var prof, TimeSpan.FromMilliseconds(1), ct);
                    if (gotProfile)
                    {
                        //TODO: check the result of Post to see if we lose profiles
                        // due to the downstream processing being too slow
                        if (!AvailableProfiles.Post(prof.ToLogScannerProfile()))
                        {
                            failedToPost++;
                            if (failedToPost >= 100)
                            {
                                string msg = "BufferBlock failed to post new profiles 100 times.";
                                logger.Error(msg);
                                throw new InternalBufferOverflowException(msg);
                            }
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // perfectly normal exception we get when using the token to cancel
        }
        catch (Exception e)
        {
            string msg = $"Encountered scanning error: {e}";
            logger!.Error(msg);
            OnScanErrorEncountered();
        }
        finally
        {
            if (scanSystem!.IsScanning)
            {
                scanSystem.StopScanning();
            }

            scanSystem.Dispose();
            scanSystem = null;

            IsRunning = false;
        }
    }

    private ScanSystem SetupScanSystem()
    {
        logger!.Debug("Setting up ScanSystem with unit: Millimeters");
        var system = new ScanSystem(ScanSystemUnits.Millimeters);
        logger.Debug($"Configuration contains {Config.ScanHeads.Count()} heads.");

        try
        {
            foreach (var headConfig in Config.ScanHeads)
            {
                logger.Debug($"Creating ScanHead for serial number {headConfig.Serial} with ID {headConfig.Id}.");
                var scanHead = system.CreateScanHead(headConfig.Serial, headConfig.Id);
                var conf = new ScanHeadConfiguration();
                logger.Debug($"Configuring head {scanHead.ID}.");
                logger.Debug($"Setting Laser Exposure for {scanHead.ID} " +
                             $"to {headConfig.MinLaserOn}/{headConfig.DefaultLaserOn}/{headConfig.MaxLaserOn} "
                             + "(min/default/max)");
                conf.SetLaserOnTime(headConfig.MinLaserOn, headConfig.DefaultLaserOn, headConfig.MaxLaserOn);
                
                conf.LaserDetectionThreshold = 150; // or higher, default is 120, max is 1023
                logger.Debug($"Setting LaserDetectionThreshold for {scanHead.ID} to {conf.LaserDetectionThreshold}");

                logger.Debug($"Applying configuration to {scanHead.ID}");
                scanHead.Configure(conf);

                logger.Debug($"Setting Window for {scanHead.ID} to {headConfig.Window}.");
                var ptList = new List<Point2D>();
                foreach (string s in headConfig.Window.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var xystr = s.Split(',');
                    if (xystr.Length != 2)
                    {
                        throw new InvalidOperationException($"Invalid window string: {s}");
                    }
                    var x = double.Parse(xystr[0]);
                    var y = double.Parse(xystr[1]);
                    ptList.Add(new Point2D(x,y,0));
                }
                scanHead.SetWindow(ScanWindow.CreateScanWindowPolygonal(ptList));

                logger.Debug($"Setting Alignment for head {scanHead.ID} to ShiftX: {headConfig.AlignmentShiftX}"
                + $" ShiftY: {headConfig.AlignmentShiftY} "
                + $" RollDeg: {headConfig.AlignmentRollDegrees}"
                + $" Orientation: {headConfig.AlignmentOrientation}");

                scanHead.Orientation = headConfig.AlignmentOrientation;

                scanHead.SetAlignment(headConfig.AlignmentRollDegrees,
                headConfig.AlignmentShiftX,
                headConfig.AlignmentShiftY);
            }
            logger.Debug("Done setting up ScanSystem");
            return system;
        }
        catch (Exception e)
        {
            logger.Error($"Failed to create ScanSystem: {e.Message}");
            throw;
        }
    }

    protected virtual void OnScanningStarted()
    {
        ScanningStarted?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnScanningStopped()
    {
        ScanningStopped?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnScanErrorEncountered()
    {
        ScanErrorEncountered?.Invoke(this, EventArgs.Empty);
    }



    private void EncoderUpdaterOnScanSyncUpdate(object? sender, EncoderUpdateArgs e)
    {
        // we just pass on the event
        EncoderUpdated.Raise(this, e);
    }
}
