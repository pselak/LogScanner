﻿using JoeScan.LogScanner.Core.Extensions;
using JoeScan.LogScanner.Core.Geometry;
using System.Runtime.ExceptionServices;
using UnitsNet;
using UnitsNet.Units;
#pragma warning disable CS0618

namespace JoeScan.LogScanner.Core.Models;

public class LogModel
{
    private readonly double maxFitError;
    private readonly double encoderPulseInterval;

    [AttributeUsage(AttributeTargets.Property)]
    public class UnitAttribute : Attribute
    {
        public QuantityType SourceUnitType { get; set; }

        public UnitAttribute(QuantityType sourceUnitType)
        {
            SourceUnitType = sourceUnitType;
        }
    }



    #region Immutable Properties

    /// <summary>
    /// Log Number
    /// </summary>
    [Unit(QuantityType.Undefined)]
    public int LogNumber { get;  }

    /// <summary>
    /// The interval between sections in millimeters.
    /// </summary>
    [Unit(QuantityType.Length)]
    public double Interval { get;  }

    /// <summary>
    /// Date and Time when this log was scanned
    /// </summary>
    [Unit(QuantityType.Undefined)]
    public DateTime TimeScanned { get;  }

    public UnitSystem Units { get; }

    /// <summary>
    /// Data and measurements at each section
    /// </summary>
    public List<LogSection> Sections { get; init; }= new List<LogSection>();

    /// <summary>
    /// Data and measurements at each section
    /// </summary>
    public  List<LogSection> RejectedSections { get; init; } = new List<LogSection>();
    
    [Unit(QuantityType.Length)]
    public double EncoderPulseInterval => encoderPulseInterval;

    #endregion

    #region Lazy Backing Values

    private Lazy<double> length;
    private Lazy<double> beginZ;
    private Lazy<double> endZ;
    private Lazy<Profile> lastGoodProfile;
    private Lazy<Profile> firstGoodProfile;
    private Lazy<Point3D> centerLineStart;
    private Lazy<Point3D> centerLineEnd;
    private Lazy<double> taper;
    private Lazy<double> taperX;
    private Lazy<double> taperY;

    #endregion

    #region Lifecycle

    internal LogModel(int logNumber, double interval, DateTime timeScanned, double maxFitError, double encoderPulseInterval, UnitSystem units)
    {
        this.maxFitError = maxFitError;
        this.encoderPulseInterval = encoderPulseInterval;
        LogNumber = logNumber;
        Interval = interval;
        TimeScanned = timeScanned;
        Units = units;
        length = new Lazy<double>(() => (LastGoodProfile.EncoderValues[0] - FirstGoodProfile.EncoderValues[0])*encoderPulseInterval);
        lastGoodProfile = new Lazy<Profile>(() =>
        {
            var section = Sections.Last();
            for (int i = section.Profiles.Count - 1; i >= 0; i--)
            {
                if (section.GetFitError(section.Profiles[i].Data.ToList()) < maxFitError)
                {
                    return section.Profiles[i];
                }
            }
            // ugh...
            return section.Profiles.Last();
        },true);
        firstGoodProfile = new Lazy<Profile>(() =>
        {
            var section = Sections.First();
            foreach (var t in section.Profiles)
            {
                if (section.GetFitError(t.Data.ToList()) < maxFitError)
                {
                    return t;
                }
            }
            return section.Profiles.First();
        }, true);
        centerLineStart = new Lazy<Point3D>(() => new Point3D(
            Sections.First().SectionCenter * CenterLineSlopeX + CenterLineInterceptXZ,
            Sections.First().SectionCenter * CenterLineSlopeY + CenterLineInterceptYZ,
            Sections.First().SectionCenter,
            0.0), true);
        centerLineEnd = new Lazy<Point3D>(() => new Point3D(
            Sections.Last().SectionCenter * CenterLineSlopeX + CenterLineInterceptXZ,
            Sections.Last().SectionCenter * CenterLineSlopeY + CenterLineInterceptYZ,
            Sections.Last().SectionCenter,
            0.0), true);
        taper = new Lazy<double>(() => (LargeEndDiameter - SmallEndDiameter) / Length,true);
        taperX = new Lazy<double>(() => (LargeEndDiameterX - SmallEndDiameterX) / Length,true);
        taperY = new Lazy<double>(() => (LargeEndDiameterY - SmallEndDiameterY) / Length,true);
    }

    #endregion

    /// <summary>
    /// Log length 
    /// </summary>
    [Unit(QuantityType.Length)]
    public double Length => length.Value;
    public Profile LastGoodProfile => lastGoodProfile.Value;
    public Profile FirstGoodProfile => firstGoodProfile.Value;
    [Unit(QuantityType.Undefined)]
    public double CenterLineSlopeX { get; internal set; }
    [Unit(QuantityType.Length)]
    public double CenterLineInterceptXZ { get; internal set; }
    [Unit(QuantityType.Undefined)]
    public double CenterLineSlopeY { get; internal set; }
    [Unit(QuantityType.Length)]
    public double CenterLineInterceptYZ { get; internal set; }
    public Point3D CenterLineStart => centerLineStart.Value;
    public Point3D CenterLineEnd => centerLineEnd.Value;
    [Unit(QuantityType.Length)] 
    public double SmallEndDiameter { get; internal set; }
    [Unit(QuantityType.Length)] 
    public double SmallEndDiameterX { get; internal set; }
    [Unit(QuantityType.Length)] 
    public double SmallEndDiameterY { get; internal set; }
    [Unit(QuantityType.Length)]
    public double LargeEndDiameter { get; internal set; }
    [Unit(QuantityType.Length)]
    public double LargeEndDiameterX { get; internal set; }
    [Unit(QuantityType.Length)]
    public double LargeEndDiameterY { get; internal set; }
    [Unit(QuantityType.Ratio)]
    public double Sweep { get; internal set; }
    
    [Unit(QuantityType.Angle)]
    public double SweepAngle { get; internal set; }
    [Unit(QuantityType.Length)]
    public double CompoundSweep { get; internal set; }
    [Unit(QuantityType.Length)]
    public double CompoundSweep90 { get; internal set; }
    [Unit(QuantityType.Ratio)]
    public double Taper => taper.Value;
    [Unit(QuantityType.Ratio)]
    public double TaperX => taperX.Value;
    [Unit(QuantityType.Ratio)]
    public double TaperY => taperY.Value;
    [Unit(QuantityType.Volume)]
    public double Volume { get; internal set; }
    [Unit(QuantityType.Volume)]
    public double BarkVolume { get; internal set; }

}
