using System.CommandLine;
using System.Diagnostics;
using System.Globalization;

class Program
{
    static async Task Main(string[] args)
    {
        // Define options for the command line
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

        // Create the root command
        var rootCommand = new RootCommand("Simple CLI program to process date and time");
        rootCommand.AddOption(dateOption);
        rootCommand.AddOption(timeOption);
        rootCommand.AddOption(secondsOption);
        rootCommand.AddOption(commandOption);

        // Set the logic to execute when the command is invoked
        rootCommand.SetHandler(
            (string? date, string? time, int? seconds, string? command) =>
            {
                ProcessArguments(date, time, seconds, command);
            },
            dateOption, timeOption, secondsOption, commandOption);

        // Show help if no arguments provided
        if (args.Length == 0)
        {
            // Explicitly trigger help message
            await rootCommand.InvokeAsync("--help");
            return;
        }

        // Handle the command-line arguments
        await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Handles the arguments and processes the date, time, and commands.
    /// </summary>
    private static void ProcessArguments(string? date, string? time, int? seconds, string? command)
    {
        DateTime targetDateTime;

        // Parse the date
        if (!string.IsNullOrEmpty(date))
        {
            targetDateTime = ParseDateWithDefaults(date);
        }
        else
        {
            targetDateTime = DateTime.Now.Date; // Default to today if no date is provided
        }

        // Parse the time
        if (!string.IsNullOrEmpty(time))
        {
            targetDateTime = targetDateTime.Add(ParseTimeWithDefaults(time));
        }
        else
        {
            // Default time to 12:00:00 AM
            targetDateTime = targetDateTime.Add(new TimeSpan(0, 0, 0));
        }

        // Add seconds if the --seconds parameter is provided
        if (seconds.HasValue)
        {
            if (string.IsNullOrEmpty(time))
            {
                var now = DateTime.Now;
                targetDateTime = new DateTime(
                    targetDateTime.Year,
                    targetDateTime.Month,
                    targetDateTime.Day,
                    now.Hour,
                    now.Minute,
                    now.Second
                );
            }

            targetDateTime = targetDateTime.AddSeconds(seconds.Value);
        }

        var nowTime = DateTime.Now;

        // Ensure target date/time is in the future
        if (targetDateTime <= nowTime)
        {
            Console.WriteLine("Target date/time must be in the future!");
            Environment.Exit(1);
        }

        // Timer logic
        StartCountdown(targetDateTime, command);
    }

    /// <summary>
    /// Starts a countdown to the target date and time.
    /// </summary>
    private static void StartCountdown(DateTime targetDateTime, string? command)
    {
        var now = DateTime.Now;
        var totalSeconds = (int)(targetDateTime - now).TotalSeconds;

        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed.TotalSeconds < totalSeconds)
        {
            var elapsedSeconds = (int)stopwatch.Elapsed.TotalSeconds;
            double percentage = (elapsedSeconds / (double)totalSeconds) * 100.0;

            // Display a progress bar
            Console.Write($"\rEndzeit: {targetDateTime} ["
                          + new string('=', (int)(percentage / 2))
                          + new string(' ', 50 - (int)(percentage / 2))
                          + $"] {percentage:F2}%");

            // Reduce CPU usage
            Thread.Sleep(50);
        }

        Console.Write($"\rEndzeit: {targetDateTime} ["
                      + new string('=', 50)
                      + $"] 100.00%");
        Console.WriteLine("\nEndzeit reached!");

        // Execute the user command
        if (!string.IsNullOrEmpty(command))
        {
            ExecuteShellCommand(command);
        }
    }

    /// <summary>
    /// Executes a shell command.
    /// </summary>
    private static void ExecuteShellCommand(string command)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
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

    /// <summary>
    /// Parses a date string and fills in missing components with defaults.
    /// </summary>
    private static DateTime ParseDateWithDefaults(string date)
    {
        if (DateTime.TryParseExact(date, new[] { "yyyy-MM-dd", "dd.MM.yyyy", "MM/yyyy", "yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            if (date.Length == 4) // Year only
            {
                return new DateTime(parsedDate.Year, 1, 1);
            }

            if (date.Length == 7) // Year and Month
            {
                return new DateTime(parsedDate.Year, parsedDate.Month, 1);
            }

            return parsedDate;
        }

        throw new FormatException("Invalid date format. Use YYYY-MM-DD, MM/YYYY, or YYYY.");
    }

    /// <summary>
    /// Parses a time string and fills in missing components with defaults.
    /// </summary>
    private static TimeSpan ParseTimeWithDefaults(string time)
    {
        if (TimeSpan.TryParseExact(time, new[] { "hh\\:mm", "hh\\:mm\\:ss" },
                CultureInfo.InvariantCulture, out var parsedTime))
        {
            if (time.Length == 5) // Hours and Minutes only
            {
                return new TimeSpan(parsedTime.Hours, parsedTime.Minutes, 0);
            }

            return parsedTime;
        }

        throw new FormatException("Invalid time format. Use HH:MM or HH:MM:SS.");
    }
}