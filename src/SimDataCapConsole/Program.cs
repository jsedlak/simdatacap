using Serilog;
using SimDataCapConsole;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var simCap = new SimCap();
simCap.Start();

bool isRunning = true;
while (!Console.KeyAvailable && isRunning)
{
    switch(Console.ReadKey(true).Key)
    {
        case ConsoleKey.Q:
            isRunning = false;
            break;
    }
}

Log.Logger.Information("Quitting...");

simCap.Stop();