using JoeScan.LogScanner.Core.Geometry;
using JoeScan.LogScanner.Core.Models;
using JoeScan.LogScanner.Core.Schema;

namespace JoeScan.LogScanner.Core.Extensions;

public static class SerializationExtensions
{
    public static LogModelT ToLogModelT(this LogModel logModel)
    {
        var t = new LogModelT
            {
                LogNumber = logModel.LogNumber,
                Interval = logModel.Interval,
                TimeScanned = logModel.TimeScanned.ToBinary(),
                Length = logModel.Length,
                EncoderPulseInterval = logModel.EncoderPulseInterval,
                FirstGoodProfile = logModel.FirstGoodProfile.ToProfileT(),
                LastGoodProfile = logModel.LastGoodProfile.ToProfileT(),
                CenterLineSlopeX = logModel.CenterLineSlopeX,
                CenterLineSlopeY = logModel.CenterLineSlopeY,
                CenterLineInterceptXZ = logModel.CenterLineInterceptXZ,
                CenterLineInterceptYZ = logModel.CenterLineInterceptYZ,
                CenterLineStart = logModel.CenterLineStart.ToPoint3DT(),
                CenterLineEnd = logModel.CenterLineEnd.ToPoint3DT(),
                SmallEndDiameter = logModel.SmallEndDiameter,
                SmallEndDiameterX = logModel.SmallEndDiameterX,
                SmallEndDiameterY = logModel.SmallEndDiameterY,
                LargeEndDiameter = logModel.LargeEndDiameter,
                LargeEndDiameterX = logModel.LargeEndDiameterX,
                LargeEndDiameterY = logModel.LargeEndDiameterY,
                Sweep = logModel.Sweep,
                SweepAngleRad = logModel.SweepAngleRad,
                CompoundSweep = logModel.CompoundSweep,
                CompoundSweep90 = logModel.CompoundSweep90,
                Taper = logModel.Taper,
                TaperX = logModel.TaperX,
                TaperY = logModel.TaperY,
                Volume = logModel.Volume,
                BarkVolume = logModel.BarkVolume,
                MaxDiameter = logModel.MaxDiameter,
                MinDiameter = logModel.MinDiameter,
                MaxDiameterZ = logModel.MaxDiameterZ,
                MinDiameterZ = logModel.MinDiameterZ,
                ButtEndFirst = logModel.ButtEndFirst,
                RawLog = logModel.RawLog.ToRawLogT(),
                Sections = new List<LogSectionT>()
            };
        foreach (var section in logModel.Sections)
        {
            t.Sections.Add(section.ToLogSectionT());
        }
        t.RejectedSections = new List<LogSectionT>();
        foreach (var section in logModel.RejectedSections)
        {
            t.RejectedSections.Add(section.ToLogSectionT());
        }
        return t;
    }

    public static LogSectionT ToLogSectionT(this LogSection logSection)
    {
        var s = new LogSectionT();
        s.IsValid = logSection.IsValid;
        s.Profiles = new List<ProfileT>();
        foreach (var profile in logSection.Profiles)
        {
            s.Profiles.Add(profile.ToProfileT());
        }

        s.SectionCenter = logSection.SectionCenter;
        s.AcceptedPoints = new List<Point2DT>();
        foreach (var point in logSection.AcceptedPoints)
        {
            s.AcceptedPoints.Add(point.ToPoint2DT());
        }
        s.ModeledProfile = new List<Point2DT>();
        foreach (var point in logSection.ModeledProfile)
        {
            s.ModeledProfile.Add(point.ToPoint2DT());
        }
        s.RejectedPoints = new List<Point2DT>();
        foreach (var point in logSection.RejectedPoints)
        {
            s.RejectedPoints.Add(point.ToPoint2DT());
        }

        s.BoundingBox = new RectT()
        {
            X = logSection.BoundingBox.FilteredMinX,
            Y = logSection.BoundingBox.FilteredMinY,
            Width = logSection.BoundingBox.FilteredMaxX - logSection.BoundingBox.FilteredMinX,
            Height = logSection.BoundingBox.FilteredMaxY - logSection.BoundingBox.FilteredMinY
        };
        s.EllipseModel = new EllipseT()
        {
            A = logSection.EllipseModel.A,
            B = logSection.EllipseModel.B,
            Theta = logSection.EllipseModel.Theta,
            X = logSection.EllipseModel.X,
            Y = logSection.EllipseModel.Y
        };
        s.BarkAllowance = logSection.BarkAllowance;
        s.RawDiameterX = logSection.RawDiameterX;
        s.RawDiameterY = logSection.RawDiameterY;
        s.TotalArea = logSection.TotalArea;
        s.WoodArea = logSection.WoodArea;
        s.FitError = logSection.FitError;
        s.RawDiameterMin = logSection.RawDiameterMin;
        s.RawDiameterMax = logSection.RawDiameterMax;
        s.CenterLineX = logSection.CenterLineX;
        s.CenterLineY = logSection.CenterLineY;

        return s;
    }

    public static RawLogT ToRawLogT(this RawLog rawLog)
    {
        var r = new RawLogT();
        r.LogNumber = rawLog.LogNumber;
        r.Id = rawLog.Id.ToString();
        r.TimeScanned = rawLog.TimeScanned.ToBinary();
        r.RawFileName = rawLog.ArchiveFileName;
        r.ProfileData = new List<ProfileT>();
        foreach (var profile in rawLog.ProfileData)
        {
            r.ProfileData!.Add(profile.ToProfileT());
        }

        return r;
    }

    public static ProfileT ToProfileT(this Profile p)
    {
        var prof = new ProfileT
        {
            Units = (UnitTypeT)p.Units,
            ScanningFlags = (ScanFlagsT)p.ScanningFlags,
            LaserIndex = p.LaserIndex,
            LaserOnTimeUs = p.LaserOnTimeUs,
            EncoderValue = p.EncoderValues[0],
            SequenceNumber = p.SequenceNumber,
            TimeStampNs = p.TimeStampNs,
            ScanHeadId = p.ScanHeadId,
            Camera = p.Camera,
            Inputs = (InputFlagsT)p.Inputs,
            BoundingBox = new RectT()
            {
                X = p.BoundingBox.X,
                Y = p.BoundingBox.Y,
                Width = p.BoundingBox.Width,
                Height = p.BoundingBox.Height
            },
            Data = new List<Point2DT>()
        };
        foreach (var point2D in p.Data)
        {
            prof.Data!.Add(point2D.ToPoint2DT());
        }

        return prof;
    }

    public static Point2DT ToPoint2DT(this Point2D p)
    {
        return new Point2DT { X = p.X, Y = p.Y, B = p.B };
    }

    public static Point3DT ToPoint3DT(this Point3D p)
    {
        return new Point3DT { X = p.X, Y = p.Y, Z = p.Z, B = p.B };
    }

    public static EllipseT ToEllipseT(this Ellipse e)
    {
        return new EllipseT { A = e.A, B = e.B, Theta = e.Theta, X = e.X, Y = e.Y };
    }
}
