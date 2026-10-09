using System.Diagnostics;

namespace Aviato.API.Endpoints;

// The workshop sidebar's "Reset application" button hits these routes.
// The database is an in-memory SQLite file that only returns to the seed data
// when the process starts again, so reset stops this process and a supervisor
// (or a replacement process) starts it once more.
public static class WorkshopReset
{
    private static readonly string InstanceId = Guid.NewGuid().ToString("n");

    public static void MapWorkshopReset(this WebApplication app)
    {
        app.MapGet("/workshop/health", (HttpResponse response) =>
        {
            response.Headers.CacheControl = "no-store";
            return Results.Ok(new { status = "ok", instanceId = InstanceId });
        });

        app.MapPost("/workshop/reset", (IHostApplicationLifetime lifetime) =>
        {
            app.Logger.LogInformation(
                "Workshop reset: restarting Aviato.API so the in-memory database is re-seeded.");

            _ = Task.Run(async () =>
            {
                try
                {
                    // Let the HTTP response leave before the port closes.
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    Restart(lifetime, app.Logger);
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "Workshop reset failed. Aviato.API was left running.");
                }
            });

            return Results.Ok(new { status = "restarting", instanceId = InstanceId });
        });
    }

    private static void Restart(IHostApplicationLifetime lifetime, ILogger logger)
    {
        var supervised = Environment.GetEnvironmentVariable("AVIATO_SUPERVISED") == "1";
        if (supervised)
        {
            WriteRestartFlag();
            logger.LogInformation("Workshop reset: asking the launch script to start Aviato.API again.");
            lifetime.StopApplication();
            return;
        }

        SpawnReplacement(logger);
        lifetime.StopApplication();
    }

    private static void WriteRestartFlag()
    {
        var stateDir = Environment.GetEnvironmentVariable("AVIATO_STATE_DIR");
        if (string.IsNullOrWhiteSpace(stateDir))
            stateDir = "/tmp/aviato-launch";

        var role = Environment.GetEnvironmentVariable("AVIATO_ROLE");
        if (string.IsNullOrWhiteSpace(role))
            role = "api";

        Directory.CreateDirectory(stateDir);
        var flag = Path.Combine(stateDir, role + ".restart");
        File.WriteAllText(flag, "1");
    }

    // `dotnet run` does not start the app again after it exits. Spawn a helper
    // that waits until this process is gone (so the port is free) and execs the
    // same binary with the same arguments and environment.
    private static void SpawnReplacement(ILogger logger)
    {
        var exe = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot locate the running executable.");

        var args = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(Quote));
        var script =
            "trap '' HUP; " +
            "while kill -0 " + Environment.ProcessId + " 2>/dev/null; do sleep 0.2; done; " +
            "cd " + Quote(Directory.GetCurrentDirectory()) + "; " +
            "exec " + Quote(exe) + (args.Length > 0 ? " " + args : "");

        var start = Process.Start(new ProcessStartInfo
        {
            FileName = "setsid",
            ArgumentList = { "sh", "-c", script },
            UseShellExecute = false,
            RedirectStandardInput = true
        }) ?? throw new InvalidOperationException("Could not start the replacement process.");

        start.StandardInput.Close();
        logger.LogInformation("Workshop reset: replacement process launched.");
    }

    private static string Quote(string value) => "'" + value.Replace("'", "'\\''") + "'";
}
