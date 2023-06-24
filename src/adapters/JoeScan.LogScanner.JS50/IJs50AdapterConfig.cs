using Config.Net;
using JoeScan.Pinchot;
using System.Security.Permissions;

namespace JoeScan.LogScanner.Js50;

public interface IJs50AdapterConfig
{
    [Option(DefaultValue = "3000")]
    public uint ScanPeriodUs { get; set; }
    [Option(DefaultValue = "XYBrightnessHalf")]
    public DataFormat DataFormat { get; set; }
    public double EncoderPulseInterval { get; set; }

    IEnumerable<IJs50HeadConfig> ScanHeads { get;  }
}

public interface IJs50HeadConfig
{
    public uint Serial { get;  }
    public uint Id { get;  }
    public uint MinLaserOn { get;  }
    public uint DefaultLaserOn { get;  }
    public uint MaxLaserOn { get;  }
   
    [Option(Alias = "Alignment.ShiftX")]
    public double AlignmentShiftX { get;  }
    [Option(Alias = "Alignment.ShiftY")]
    public double AlignmentShiftY { get;  }
    [Option(Alias = "Alignment.RollDegrees")]
    public double AlignmentRollDegrees { get;  }
    [Option (Alias ="Alignment.Orientation")]
    public ScanHeadOrientation AlignmentOrientation { get;  }
    [Option(Alias = "Window.Polygon")]
    public string Window { get; }
}
