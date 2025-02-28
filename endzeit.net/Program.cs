using System.CommandLine;
using System.Diagnostics;
using System.Globalization;

class Program
{
    static async Task Main(string[] args)
    {
        var dateOption = new Option<string?>(
            "--date",
            "Date in the format YYYY-MM-DD, MM/YYYY, or YYYY (optional, defaults to today)");
        
        var timeOption = new Option<string?>(
            "--time",
            "Time in the format HH:MM or HH:MM:SS (optional, defaults to 12:00:00 AM)");
        
        var secondsOption = new Option<int?>(
            "--seconds",
            "Adds the specified number of seconds to the current date and time (optional)");

        var commandOption = new Option<string?>(
            "--command",
            "A command to execute after the Endzeit is reached (optional)");

        var rootCommand = new RootCommand("Simple CLI program to process date and time");
        rootCommand.AddOption(dateOption);
        rootCommand.AddOption(timeOption);
        rootCommand.AddOption(secondsOption);
        rootCommand.AddOption(commandOption);

        rootCommand.SetHandler((string? date, string? time, int? seconds, string? command) =>
{
    DateTime targetDateTime;

    // Parse the date with default handling
    if (!string.IsNullOrEmpty(date))
    {
        targetDateTime = ParseDateWithDefaults(date);
    }
    else
    {
        targetDateTime = DateTime.Now.Date; // Default to today if no date is provided
    }

    // Parse the time with default handling
    if (!string.IsNullOrEmpty(time))
    {
        targetDateTime = targetDateTime.Add(ParseTimeWithDefaults(time));
    }
    else
    {
        targetDateTime = targetDateTime.Add(new TimeSpan(0, 0, 0)); // Default time to 12:00:00 AM
    }

    // Add seconds if the --seconds parameter is provided
    if (seconds.HasValue)
    {
        // If no specific time was provided, use the current hour and minute
        if (string.IsNullOrEmpty(time))
        {
            var now2 = DateTime.Now;
            targetDateTime = new DateTime(
                targetDateTime.Year,
                targetDateTime.Month,
                targetDateTime.Day,
                now2.Hour,
                now2.Minute,
                now2.Second
            );
        }

        // Add the specified seconds
        targetDateTime = targetDateTime.AddSeconds(seconds.Value);
    }

    var now = DateTime.Now;

    // Ensure the target date and time is in the future
    if (targetDateTime <= now)
    {
        Console.WriteLine("Target date/time must be in the future!");
        Environment.Exit(1);
    }

    // Timer logic
    var totalSeconds = (int)(targetDateTime - now).TotalSeconds;

    // Use Stopwatch for precise timing
    var stopwatch = Stopwatch.StartNew();

    while (stopwatch.Elapsed.TotalSeconds < totalSeconds)
    {
        // Calculate elapsed seconds
        var elapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds;

        // Calculate percentage of completion
        double percentage = (elapsedSeconds / (double)totalSeconds) * 100.0;

        // Display the progress bar
        Console.Write($"\rEndzeit: {targetDateTime} [" 
                      + new string('=', (int)(percentage / 2)) 
                      + new string(' ', 50 - (int)(percentage / 2)) 
                      + $"] {percentage:F2}%");

        // Use a small delay to reduce CPU usage while keeping updates frequent
        Thread.Sleep(50);
    }

    // Ensure the progress bar reaches exactly 100% before finishing
    Console.Write($"\rEndzeit: {targetDateTime} [" 
                  + new string('=', 50) 
                  + $"] 100.00%");

    Console.WriteLine("\nEndzeit reached!");

    // Execute the user-defined command
    if (!string.IsNullOrEmpty(command))
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"", // Execute command in a shell
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new Exception("Failed to start the command.");
            }

            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to execute the command: {ex.Message}");
        }
    }
}, dateOption, timeOption, secondsOption, commandOption);

        await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Parses a date string and fills in defaults for missing components.
    /// </summary>
    private static DateTime ParseDateWithDefaults(string date)
    {
        if (DateTime.TryParseExact(date, new[] { "yyyy-MM-dd", "MM/yyyy", "yyyy" }, 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            // Fill in missing components
            if (date.Length == 4) // Year only
            {
                return new DateTime(parsedDate.Year, 1, 1); // Defaults to January 1st
            }
            if (date.Length == 7) // Year and Month
            {
                return new DateTime(parsedDate.Year, parsedDate.Month, 1); // Defaults to the 1st of the month
            }
            return parsedDate; // Fully specified date
        }
        
        throw new FormatException("Invalid date format. Use YYYY-MM-DD, MM/YYYY, or YYYY.");
    }

    /// <summary>
    /// Parses a time string and fills in defaults for missing components.
    /// </summary>
    private static TimeSpan ParseTimeWithDefaults(string time)
    {
        if (TimeSpan.TryParseExact(time, new[] { "hh\\:mm", "hh\\:mm\\:ss" }, CultureInfo.InvariantCulture, out var parsedTime))
        {
            // Fill in missing components
            if (time.Length == 5) // Hours and Minutes only
            {
                return new TimeSpan(parsedTime.Hours, parsedTime.Minutes, 0); // Defaults seconds to 0
            }
            return parsedTime; // Fully specified time
        }
        
        throw new FormatException("Invalid time format. Use HH:MM or HH:MM:SS.");
    }
}