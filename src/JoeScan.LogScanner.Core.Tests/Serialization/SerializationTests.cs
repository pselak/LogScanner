using Autofac;
using FlatSharp;
using JoeScan.LogScanner.Core.Extensions;
using JoeScan.LogScanner.Core.Geometry;
using JoeScan.LogScanner.Core.Models;
using JoeScan.LogScanner.Core.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace JoeScan.LogScanner.Core.Tests.Serialization;
[TestClass]
public class SerializationTests
{
    [TestMethod]
    public void TestEllipseT()
    {
        var e = new Ellipse(new[] { 19.0, 17.0, 21.0, 23.0, 25.0 });
        var sut = e.ToEllipseT();
        Assert.AreEqual(19.0, sut.A); // A is always the larger of the two radii
        Assert.AreEqual(17.0, sut.B);
        Assert.AreEqual(21.0, sut.Theta);
        Assert.AreEqual(23.0, sut.X);
        Assert.AreEqual(25.0, sut.Y);
        int maxBytesNeeded = EllipseT.Serializer.GetMaxSize(sut);
        byte[] buffer = new byte[maxBytesNeeded]; 
        int bytesWritten = EllipseT.Serializer.Write(buffer,sut);

        var sut2 = EllipseT.Serializer.Parse(buffer);
        Assert.AreEqual(sut.A, sut2.A);
        Assert.AreEqual(sut.B, sut2.B);
        Assert.AreEqual(sut.Theta, sut2.Theta);
        Assert.AreEqual(sut.X, sut2.X);
        Assert.AreEqual(sut.Y, sut2.Y);
    }

    [TestMethod]
    public void TestProfileT()
    {
        var p = new Profile();
        p.Data = new Point2D[] { new Point2D(1.0, 2.0, 3.0), new Point2D(4.0, 5.0, 6.0) };
        p.Units = UnitSystem.Millimeters;
        p.ScanningFlags = ScanFlags.InternalError | ScanFlags.Overrun;
        p.LaserIndex = 1;
        p.LaserOnTimeUs = 777;
        p.EncoderValues = new Dictionary<uint, long>() { { 0, 2133458L } };
        p.SequenceNumber = 1234567890;
        p.TimeStampNs = UInt64.MaxValue;
        p.ScanHeadId = 17;
        p.Camera = 1;
        p.Inputs = InputFlags.EncoderA | InputFlags.EncoderB;
        p.BoundingBox = new Rect(1.0, 2.0, 3.0, 4.0);

        var sut = p.ToProfileT();
        Assert.AreEqual(2, sut.Data.Count);
        Assert.AreEqual(1.0, sut.Data[0].X);
        Assert.AreEqual(2.0, sut.Data[0].Y);
        Assert.AreEqual(3.0, sut.Data[0].B);
        Assert.AreEqual(4.0, sut.Data[1].X);
        Assert.AreEqual(5.0, sut.Data[1].Y);
        Assert.AreEqual(6.0, sut.Data[1].B);
        Assert.AreEqual(UnitSystem.Millimeters, (UnitSystem) sut.Units);
        Assert.AreEqual(ScanFlags.InternalError | ScanFlags.Overrun, (ScanFlags) sut.ScanningFlags);
        Assert.AreEqual(1u, sut.LaserIndex);
        Assert.AreEqual(777, sut.LaserOnTimeUs);
        Assert.AreEqual(2133458L, sut.EncoderValue);
        Assert.AreEqual(1234567890u, sut.SequenceNumber);
        Assert.AreEqual(17u, sut.ScanHeadId);
        Assert.AreEqual(1u, sut.Camera);
        Assert.AreEqual(InputFlags.EncoderA | InputFlags.EncoderB, (InputFlags) sut.Inputs);
        Assert.AreEqual(1.0, sut.BoundingBox.X);
        Assert.AreEqual(2.0, sut.BoundingBox.Y);
        Assert.AreEqual(3.0, sut.BoundingBox.Width);
        Assert.AreEqual(4.0, sut.BoundingBox.Height);
        Assert.AreEqual(UInt64.MaxValue, sut.TimeStampNs);

        int maxBytesNeeded = ProfileT.Serializer.GetMaxSize(sut);
        byte[] buffer = new byte[maxBytesNeeded];
        int bytesWritten = ProfileT.Serializer.Write(buffer, sut);

        sut = ProfileT.Serializer.Parse(buffer);
        Assert.AreEqual(2, sut.Data.Count);
        Assert.AreEqual(1.0, sut.Data[0].X);
        Assert.AreEqual(2.0, sut.Data[0].Y);
        Assert.AreEqual(3.0, sut.Data[0].B);
        Assert.AreEqual(4.0, sut.Data[1].X);
        Assert.AreEqual(5.0, sut.Data[1].Y);
        Assert.AreEqual(6.0, sut.Data[1].B);
        Assert.AreEqual(UnitSystem.Millimeters, (UnitSystem)sut.Units);
        Assert.AreEqual(ScanFlags.InternalError | ScanFlags.Overrun, (ScanFlags)sut.ScanningFlags);
        Assert.AreEqual(1u, sut.LaserIndex);
        Assert.AreEqual(777, sut.LaserOnTimeUs);
        Assert.AreEqual(2133458L, sut.EncoderValue);
        Assert.AreEqual(1234567890u, sut.SequenceNumber);
        Assert.AreEqual(17u, sut.ScanHeadId);
        Assert.AreEqual(1u, sut.Camera);
        Assert.AreEqual(InputFlags.EncoderA | InputFlags.EncoderB, (InputFlags)sut.Inputs);
        Assert.AreEqual(1.0, sut.BoundingBox.X);
        Assert.AreEqual(2.0, sut.BoundingBox.Y);
        Assert.AreEqual(3.0, sut.BoundingBox.Width);
        Assert.AreEqual(4.0, sut.BoundingBox.Height);
        Assert.AreEqual(UInt64.MaxValue, sut.TimeStampNs);
    }

    [TestMethod]
    public void TestRawLogT()
    {
        var rawLog = RawLogReaderWriter.Read("RawLog_7441.loga");
        var sut = rawLog.ToRawLogT();

        Assert.AreEqual(7441, sut.LogNumber);
        Assert.AreEqual(rawLog.TimeScanned, DateTime.FromBinary(sut.TimeScanned));
        Assert.AreEqual(rawLog.Id, Guid.Parse(sut.Id!));
        Assert.AreEqual(rawLog.ProfileData.Count, sut.ProfileData!.Count);
        
        int maxBytesNeeded = RawLogT.Serializer.GetMaxSize(sut);
        byte[] buffer = new byte[maxBytesNeeded];
        int bytesWritten = RawLogT.Serializer.Write(buffer, sut);

        sut = RawLogT.Serializer.Parse(buffer);
        Assert.AreEqual(7441, sut.LogNumber);
        Assert.AreEqual(rawLog.TimeScanned, DateTime.FromBinary(sut.TimeScanned));
        Assert.AreEqual(rawLog.Id, Guid.Parse(sut.Id!));
        Assert.AreEqual(rawLog.ProfileData.Count, sut.ProfileData!.Count);
    }

    [TestMethod]
    public void TestLogModel()
    {
        var rawLog = RawLogReaderWriter.Read("RawLog_7441.loga");
        var builder = new ContainerBuilder();
        builder.RegisterModule<CoreModule>();
        var container = builder.Build();

        using var scope = container.BeginLifetimeScope();
        var logModelBuilder = scope.Resolve<LogModelBuilder>();
        var res = logModelBuilder.Build(rawLog);

        var lm = res.LogModel;
        var sut = lm!.ToLogModelT();

        int maxBytesNeeded = LogModelT.Serializer.GetMaxSize(sut);
        byte[] buffer = new byte[maxBytesNeeded];
        int bytesWritten = LogModelT.Serializer.Write(buffer, sut);

        sut = LogModelT.Serializer.Parse(buffer);
        Assert.AreEqual(lm!.LogNumber, sut.LogNumber);
        Assert.AreEqual(lm.Interval, sut.Interval);
        Assert.AreEqual(lm.TimeScanned, DateTime.FromBinary(sut.TimeScanned));
        Assert.AreEqual(lm.EncoderPulseInterval, sut.EncoderPulseInterval);
        Assert.AreEqual(lm.Length, sut.Length);
        Assert.AreEqual(lm.CenterLineSlopeX, sut.CenterLineSlopeX);
        Assert.AreEqual(lm.CenterLineSlopeY, sut.CenterLineSlopeY);
        Assert.AreEqual(lm.CenterLineInterceptXZ, sut.CenterLineInterceptXZ);
        Assert.AreEqual(lm.CenterLineInterceptYZ, sut.CenterLineInterceptYZ);
        Assert.AreEqual(lm.SmallEndDiameter, sut.SmallEndDiameter);
        Assert.AreEqual(lm.SmallEndDiameterX, sut.SmallEndDiameterX);
        Assert.AreEqual(lm.SmallEndDiameterY, sut.SmallEndDiameterY);
        Assert.AreEqual(lm.LargeEndDiameter, sut.LargeEndDiameter);
        Assert.AreEqual(lm.LargeEndDiameterX, sut.LargeEndDiameterX);
        Assert.AreEqual(lm.LargeEndDiameterY, sut.LargeEndDiameterY);
        Assert.AreEqual(lm.Sweep, sut.Sweep);
        Assert.AreEqual(lm.SweepAngleRad, sut.SweepAngleRad);
        Assert.AreEqual(lm.CompoundSweep, sut.CompoundSweep);
        Assert.AreEqual(lm.CompoundSweep90, sut.CompoundSweep90);
        Assert.AreEqual(lm.Taper, sut.Taper);
        Assert.AreEqual(lm.TaperX, sut.TaperX);
        Assert.AreEqual(lm.TaperY, sut.TaperY);
        Assert.AreEqual(lm.Volume, sut.Volume);
        Assert.AreEqual(lm.BarkVolume, sut.BarkVolume);
        Assert.AreEqual(lm.MaxDiameter, sut.MaxDiameter);
        Assert.AreEqual(lm.MaxDiameterZ, sut.MaxDiameterZ);
        Assert.AreEqual(lm.MinDiameter, sut.MinDiameter);
        Assert.AreEqual(lm.MinDiameterZ, sut.MinDiameterZ);
        Assert.AreEqual(lm.ButtEndFirst, sut.ButtEndFirst);

        Assert.AreEqual(lm.Sections.Count, sut.Sections!.Count);
        Assert.AreEqual(lm.RejectedSections.Count, sut.RejectedSections!.Count);

        // foreach (var tup in sut.Sections.Zip(lm.Sections, (i1, i2) => Tuple.Create(i1, i2)))
        // {
        //     Assert.AreEqual(tup.Item1.AcceptedPoints!.Count, tup.Item2.AcceptedPoints.Count);
        // }

    }
}


