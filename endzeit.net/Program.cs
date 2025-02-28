using System;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var dateOption = new Option<string?>(
            "--date",
            "Date in the format YYYY-MM-DD or MM/DD/YYYY (optional, defaults to today)");
        
        var timeOption = new Option<string?>(
            "--time",
            "Time in the format HH:MM:SS (optional, defaults to 12:00 AM)");
        
        var commandOption = new Option<string?>(
            "--command",
            "A user-defined command to execute after the Endzeit is reached (optional)");

        var rootCommand = new RootCommand("Simple CLI program to process date and time");
        rootCommand.AddOption(dateOption);
        rootCommand.AddOption(timeOption);
        rootCommand.AddOption(commandOption);

        rootCommand.SetHandler((string? date, string? time, string? command) =>
        {
            DateTime targetDateTime;

            // Parse the date
            if (!string.IsNullOrEmpty(date))
            {
                string[] validFormats = { "yyyy-MM-dd", "MM/dd/yyyy" };
                if (!DateTime.TryParseExact(date, validFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    Console.WriteLine("Invalid date format. Use YYYY-MM-DD or MM/DD/YYYY.");
                    Environment.Exit(1);
                }
                targetDateTime = parsedDate;
            }
            else
            {
                targetDateTime = DateTime.Now.Date; // Default to today if no date is provided
            }

            // Parse the time and default to 12:00 AM if not provided
            if (!string.IsNullOrEmpty(time))
            {
                if (!TimeSpan.TryParse(time, out var parsedTime))
                {
                    Console.WriteLine("Invalid time format. Use HH:MM:SS.");
                    Environment.Exit(1);
                }
                targetDateTime = targetDateTime.Add(parsedTime);
            }
            else
            {
                targetDateTime = targetDateTime.AddHours(0); // Default time to 12:00 AM
            }

            var now = DateTime.Now;

            // Ensure the target date and time is in the future
            if (targetDateTime <= now)
            {
                Console.WriteLine("Target date/time must be in the future");
                Environment.Exit(1);
            }

            // Timer logic
            var totalSeconds = (int)(targetDateTime - now).TotalSeconds;

            for (int elapsed = 0; elapsed <= totalSeconds; elapsed++)
            {
                double percentage = (elapsed / (double)totalSeconds) * 100.0;
                Console.Write($"\rEndzeit: {targetDateTime} [" + new string('=', (int)(percentage / 2)) + new string(' ', 50 - (int)(percentage / 2)) + $"] {percentage:F2}%");
                Thread.Sleep(1000);
            }

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

                    // Print the output of the command
                    // Console.WriteLine($"Command Output:\n{process.StandardOutput.ReadToEnd()}");
                    // Console.WriteLine($"Command Error (if any):\n{process.StandardError.ReadToEnd()}");
                    // Console.WriteLine($"Command exited with code {process.ExitCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to execute the command: {ex.Message}");
                }
            }

        }, dateOption, timeOption, commandOption);

        await rootCommand.InvokeAsync(args);
    }
}