using System;
using System.Collections.Generic;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RateListener.Models;

public class StoredRatesContainer
{
    public Guid ListenerId { get; set; }
    public string BankProvider { get; set; }
    public List<DirectionInfo> Directions { get; set; } = [];
}

public class DirectionInfo
{
    public string Direction { get; set; }
    public List<StoredRate> Rates { get; set; } = [];
}

public class StoredRate
{
    public string Time { get; set; }
    public string Rate { get; set; }
    public string InversedRate { get; set; }
}