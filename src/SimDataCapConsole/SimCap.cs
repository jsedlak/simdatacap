using HerboldRacing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimDataCapConsole;

internal class SimCap
{
    private readonly IRSDKSharper _irSdk;

    private List<object> _data = new();

    public SimCap()
    {
        _irSdk = new IRSDKSharper();
        _irSdk.OnConnected += OnConnected;
        _irSdk.OnDisconnected += OnDisconnected;
        _irSdk.OnSessionInfo += OnSessionInfo;
        _irSdk.OnTelemetryData += OnTelemetryData;
    }

    public void Start()
    {
        _irSdk.Start();
    }

    public void Stop()
    {
        _irSdk.Stop();
    }

    private void OnTelemetryData()
    {
        var lapDistPct = _irSdk.Data.GetInt("")
            //.Data.GetFloat("CarIdxLapDistPct", 5);

        _data.Add(new
        {
            track = _irSdk.Data.SessionInfo?.WeekendInfo.TrackName,

        })
    }

    private void OnSessionInfo()
    {
        throw new NotImplementedException();
    }

    private void OnDisconnected()
    {
        throw new NotImplementedException();
    }

    private void OnConnected()
    {
        
    }
}
