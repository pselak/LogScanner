﻿using JoeScan.LogScanner.Core.Filters;
using JoeScan.LogScanner.Core.Models;

namespace JoeScan.LogScanner.Core.Interfaces;

public interface IFlightsAndWindowFilter
{
    Profile Apply(Profile p);
    IEnumerable<uint> FilteredHeads { get; }
    //TODO: this depends on concrete type, fix
    public PolygonFilter this[uint key]
    {
        get;
    }
}


