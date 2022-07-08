﻿using Caliburn.Micro;
using HelixToolkit.Wpf;
using JoeScan.LogScanner.Core.Models;
using JoeScan.LogScanner.Shared.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Color = System.Drawing.Color;

// ReSharper disable MemberCanBePrivate.Global

namespace JoeScan.LogScanner.LogReview.Log3D;

public class Log3DViewModel : Screen
{
    private LogModel? currentLogModel;
    private bool showRawPoints = false;
    private bool showModelPoints = true;
    private bool showModel = true;
    private bool needsFit = true;
    private bool showSectionCenters = true;
    private ModelVisual3D? rawPointCloud;
    private ModelVisual3D? modelPoints;
    private ModelVisual3D? model;
    private ModelVisual3D? sectionCenters;
    private HelixViewport3D Viewport { get; set; }

    #region IViewAware Implementation

    protected override void OnViewAttached(object view, object context)
    {
        if (view is Log3DView lv)
        {
            Viewport = lv.Viewport;
            // Viewport.Orthographic = true;
        }
    }

    #endregion

    public LogModel? CurrentLogModel
    {
        get => currentLogModel;
        set
        {
            if (Equals(value, currentLogModel))
            {
                return;
            }
            currentLogModel = value;
            NotifyOfPropertyChange(() => CurrentLogModel);
            CreateVisuals();
            RefreshDisplay();
        }
    }

    public bool ShowRawPoints
    {
        get => showRawPoints;
        set
        {
            if (value == showRawPoints)
            {
                return;
            }
            showRawPoints = value;
            NotifyOfPropertyChange(() => ShowRawPoints);
            RefreshDisplay();
        }
    }

    public bool ShowModelPoints
    {
        get => showModelPoints;
        set
        {
            if (value == showModelPoints)
            {
                return;
            }
            showModelPoints = value;
            NotifyOfPropertyChange(() => ShowModelPoints);
            RefreshDisplay();
        }
    }

    public bool ShowModel
    {
        get => showModel;
        set
        {
            if (value == showModel)
            {
                return;
            }
            showModel = value;
            NotifyOfPropertyChange(() => ShowModel);
            RefreshDisplay();

        }
    }
    public bool ShowSectionCenters
    {
        get => showSectionCenters;
        set
        {
            if (value == showSectionCenters)
            {
                return;
            }
            showSectionCenters = value;
            NotifyOfPropertyChange(() => ShowSectionCenters);
            RefreshDisplay();

        }
    }

    private void CreateVisuals()
    {
        rawPointCloud = CreateRawCloudByColor();
        modelPoints = CreateModelPoints();
        model = CreateModel();
        sectionCenters = CreateSectionCenters();

    }

    private ModelVisual3D CreateRawCloudByHeadId()
    {
        var firstEncVal = CurrentLogModel.Sections.First().Profiles.First().EncoderValues[0];

        var profiles = CurrentLogModel.Sections.SelectMany(q => q.Profiles).GroupBy(g => g.ScanHeadId);
        var group = new ModelVisual3D();

        foreach (var grp in profiles)
        {
            var pts = new Point3DCollection(grp.SelectMany(p => p.Data.Select(r => new Point3D(r.X, r.Y,
                (p.EncoderValues[0] - firstEncVal) * CurrentLogModel.EncoderPulseInterval))));

            var visual = new PointsVisual3D { Color = ColorDefinitions.ColorForCableId(grp.Key), Size = 1, Points = pts };
            group.Children.Add(visual);
        }
        return group;
    }

    private ModelVisual3D CreateRawCloudByColor()
    {
        var firstEncVal = CurrentLogModel.Sections.First().Profiles.First().EncoderValues[0];
        var ptsDict = new Dictionary<byte, IList<Point3D>>();
        foreach (var logSection in CurrentLogModel.Sections)
        {
            foreach (var profile in logSection.Profiles)
            {
                var z = (profile.EncoderValues[0] - firstEncVal) * CurrentLogModel.EncoderPulseInterval;
                foreach (var point2D in profile.Data)
                {
                    var pt3d = new Point3D(point2D.X, point2D.Y, z);
                    var colorValue = BinByBrightness(point2D.B);
                    if (!ptsDict.ContainsKey(colorValue))
                    {
                        ptsDict[colorValue] = new List<Point3D>();
                    }
                    ptsDict[colorValue].Add(pt3d);
                }
            }
        }
        var group = new ModelVisual3D();

        foreach (byte col in ptsDict.Keys)
        {
            var visual = new PointsVisual3D { Color = ColorDefinitions.LogColorValues[col], Size = 2, Points = new Point3DCollection(ptsDict[col]) };
            group.Children.Add(visual);
        }

        return group;
    }

    private ModelVisual3D CreateModelPoints()
    {
        var group = new ModelVisual3D();
        var pts = new Point3DCollection(CurrentLogModel.Sections.SelectMany(q =>
            q.ModeledProfile.Select(r => new Point3D(r.X, r.Y, q.SectionCenter))));
        group.Children.Add(new PointsVisual3D() { Color = Colors.Yellow, Size = 2, Points = pts });
        return group;
    }

    private ModelVisual3D CreateModel()
    {
        var group = new ModelVisual3D();
        var mb = new MeshBuilder(true);
        for (int i = 0; i < CurrentLogModel.Sections.Count - 1; i++)
        {
            var s1 = CurrentLogModel.Sections[i].ModeledProfile;
            var s2 = CurrentLogModel.Sections[i + 1].ModeledProfile;
            var modelPtCount = CurrentLogModel.Sections[i].ModeledProfile.Count;
            for (int j = 0; j < modelPtCount; j++)
            {
                var li = (j - 1 + modelPtCount) % modelPtCount;
                var ri = (j + 1) % modelPtCount;

                var p0 = new Point3D(s1[li].X, s1[li].Y, CurrentLogModel.Sections[i].SectionCenter);
                var p1 = new Point3D(s2[li].X, s2[li].Y, CurrentLogModel.Sections[i + 1].SectionCenter);
                var p2 = new Point3D(s2[ri].X, s2[ri].Y, CurrentLogModel.Sections[i + 1].SectionCenter);
                var p3 = new Point3D(s1[ri].X, s1[ri].Y, CurrentLogModel.Sections[i].SectionCenter);
                // mb.AddTriangle(p0, p3, p2);
                // mb.AddTriangle(p2, p1, p0);

                mb.AddQuad(p0, p1, p2, p3);
            }

        }
        var mesh = mb.ToMesh(true);
        group.Children.Add(new MeshGeometryVisual3D()
        {
            MeshGeometry = mesh,
            Visible = true,
            Material = MaterialHelper.CreateMaterial(new SolidColorBrush(Colors.Orange), 100, 100, 255, true),
            // BackMaterial = MaterialHelper.CreateMaterial(new SolidColorBrush(Colors.Orange), 100, 100, 255, true)
        });
        return group;
    }

    private ModelVisual3D CreateSectionCenters()
    {
        var group = new ModelVisual3D();
        for (int i = 0; i < CurrentLogModel!.Sections.Count - 1; i++)
        {
            var section = CurrentLogModel.Sections[i];
            var visual = new LinesVisual3D() { Color = Colors.Orange };
            group.Children.Add(visual);
            //TODO: Units
            visual.Points = new Point3DCollection()
            {
                new Point3D(section.CentroidX - 5, section.CentroidY, section.SectionCenter),
                new Point3D(section.CentroidX + 5, section.CentroidY, section.SectionCenter)
            };
            visual = new LinesVisual3D() { Color = Colors.Orange };
            group.Children.Add(visual);
            visual.Points = new Point3DCollection()
            {
                new Point3D(section.CentroidX, section.CentroidY-5, section.SectionCenter),
                new Point3D(section.CentroidX, section.CentroidY+5, section.SectionCenter)
            };

        }
        return group;
    }
    private static byte BinByBrightness(double b)
    {
        return (byte)(b); // clamp?
    }

    private void RefreshDisplay()
    {
        Viewport.Children.Clear();
        if (CurrentLogModel == null)
        {
            
            return;
        }

        if (rawPointCloud != null)
        {
            if (ShowRawPoints)
            {
                Viewport.Children.Add(rawPointCloud);
            }
            else
            {
                Viewport.Children.Remove(rawPointCloud);
            }
        }

        if (modelPoints != null)
        {
            if (ShowModelPoints)
            {
                Viewport.Children.Add(modelPoints);
            }
            else
            {
                Viewport.Children.Remove(modelPoints);
            }
        }

        if (model != null)
        {
            if (ShowModel)
            {
                Viewport.Children.Add(model);
            }
            else
            {
                Viewport.Children.Remove(model);
            }
        }

        if (sectionCenters != null)
        {
            if (ShowSectionCenters)
            {
                Viewport.Children.Add(sectionCenters);
            }
            else
            {
                Viewport.Children.Remove(sectionCenters);
            }
        }

        if (needsFit)
        {
            Viewport.CameraController.CameraPosition = new(2000, 300, CurrentLogModel.Length / 2);
            Viewport.FitView(new Vector3D(-2000, 300.0, CurrentLogModel.Length / 2.0), new Vector3D(0.0, 1.0, 0.0));
            needsFit = false;
        }
    }
}
