using Serilog;

using HerboldRacing;
using System.Text;
using CsvHelper;
using System.Globalization;

namespace SimDataCapConsole;

internal class SimCap
{
    private readonly IRSDKSharper _irSdk;

    private List<object> _data = new();
    private int _currentLapsCompleted = 0;
    private bool _waitingForLastLap = false;
    private float _currentLastLapTime = 0f;
    private string _sessionId = "";

    public SimCap()
    {
        _irSdk = new IRSDKSharper();
        _irSdk.OnConnected += OnConnected;
        _irSdk.OnDisconnected += OnDisconnected;
        _irSdk.OnSessionInfo += OnSessionInfo;
        _irSdk.OnTelemetryData += OnTelemetryData;
        _irSdk.UpdateInterval = 30;
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
        if(string.IsNullOrWhiteSpace(_sessionId))
        {
            Log.Logger.Warning("No session id");
            return;
        }

        var playerIdx = _irSdk.Data.GetInt("PlayerCarIdx");

        var isOnTrack = _irSdk.Data.GetBool("IsOnTrack");
        var isInPits = _irSdk.Data.GetBool("CarIdxOnPitRoad", playerIdx);

        if (!isOnTrack || isInPits)
        {
            // Log.Logger.Information("Not on track {@IsOnTrack} or in pits {@IsInPits}", isOnTrack, isInPits);
            return;
        }

        var lapsCompleted = _irSdk.Data.GetInt("CarIdxLapCompleted", playerIdx);

        // Log.Logger.Information("{@LapsCompleted} comp / {@CurrentLapsCompleted}", lapsCompleted, _currentLapsCompleted);

        if (lapsCompleted > _currentLapsCompleted)
        {
            _currentLapsCompleted = lapsCompleted;
            _waitingForLastLap = true;

            Log.Logger.Information($"Laps Completed: {_currentLapsCompleted}");
        }

        var lastLapTime = _irSdk.Data.GetFloat("CarIdxLastLapTime", playerIdx);
        var driverTrackPct = _irSdk.Data.GetFloat("CarIdxLapDistPct", playerIdx);

        var logNewLap = lastLapTime != _currentLastLapTime || driverTrackPct > 0.25f;

        if (_waitingForLastLap)
        {
            if (logNewLap)
            {
                _data.Add(new
                {
                    lap = _irSdk.Data.GetInt("CarIdxLap", playerIdx),
                    incidents = _irSdk.Data.GetInt("PlayerCarTeamIncidentCount"),
                    tireCompound = _irSdk.Data.GetInt("PlayerTireCompound"),
                    trackTemp = _irSdk.Data.GetFloat("TrackTemp"),
                    airTemp = _irSdk.Data.GetFloat("AirTemp"),
                    wetness = _irSdk.Data.GetInt("TrackWetness"),
                    skies = _irSdk.Data.GetInt("Skies"),
                    airDensity = _irSdk.Data.GetFloat("AirDensity"),
                    airPressure = _irSdk.Data.GetFloat("AirPressure"),
                    windVelocity = _irSdk.Data.GetFloat("WindVel"),
                    windDirection = _irSdk.Data.GetFloat("WindDir"),
                    humidity = _irSdk.Data.GetFloat("RelativeHumidity"),
                    fuelLevelPct = _irSdk.Data.GetFloat("FuelLevelPct"),
                    fuelTotal = _irSdk.Data.SessionInfo?.DriverInfo.DriverCarFuelMaxLtr,
                    carId = _irSdk.Data.SessionInfo?.DriverInfo.Drivers[playerIdx].CarID,
                    carPath = _irSdk.Data.SessionInfo?.DriverInfo.Drivers[playerIdx].CarPath,
                    carName = _irSdk.Data.SessionInfo?.DriverInfo.Drivers[playerIdx].CarScreenName,
                    carNameShort = _irSdk.Data.SessionInfo?.DriverInfo.Drivers[playerIdx].CarScreenNameShort,
                    fuelUserPerHour = _irSdk.Data.GetFloat("FuelUsePerHour"),
                    lastLapTime
                });

                Log.Logger.Information("Lap Logged: {@LastLapTime}", lastLapTime);

                _waitingForLastLap = false;
                _currentLastLapTime = lastLapTime;
            }
            else
            {
                Log.Logger.Information("Waiting ({@WaitingForLastLap}) - New Last Lap: {@LastLapTime} vs. Previous {@PreviousLapTime}", _waitingForLastLap, lastLapTime, _currentLastLapTime);
            }
        }

        //.Data.GetFloat("CarIdxLapDistPct", 5);

        //_data.Add(new
        //{
        //    track = _irSdk.Data.SessionInfo?.WeekendInfo.TrackName,

        //});
    }

    private void OnSessionInfo()
    {
        if (!string.IsNullOrWhiteSpace(_sessionId))
        {
            return;
        }

        _sessionId = $"{_irSdk.Data.SessionInfo!.WeekendInfo.SessionID}-{_irSdk.Data.SessionInfo!.WeekendInfo.SubSessionID}";

        Log.Logger.Information("SessionInfo logged for {@SessionId}", _sessionId);
    }

    private void OnDisconnected()
    {
        Log.Logger.Information("Disconnected");

        Flush();
    }

    public void Flush()
    {
        if(!_data.Any())
        {
            return;
        }

        using TextWriter writer = new StreamWriter(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{_sessionId}.csv"),
            false,
            Encoding.UTF8
        );

        var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(_data); // where values implements IEnumerable
    }

    private void OnConnected()
    {
        Log.Logger.Information("Connected");

        _sessionId = string.Empty;
        _currentLapsCompleted = 0;
        _data.Clear();
    }
}
