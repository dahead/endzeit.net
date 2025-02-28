using System;
using System.CommandLine;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var dateOption = new Option<string?>(
            "--date",
            "Date in the format YYYY-MM-DD (optional, defaults to today)");

        var timeOption = new Option<string>(
            "--time",
            "Time in the format HH:MM:SS (required)")
        { 
            IsRequired = true 
        };

        var rootCommand = new RootCommand("Simple CLI program to process date and time");
        rootCommand.AddOption(dateOption);
        rootCommand.AddOption(timeOption);

        rootCommand.SetHandler((string? date, string time) =>
        {
            DateTime targetDateTime;
            if (!string.IsNullOrEmpty(date))
            {
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    Console.WriteLine("Invalid date format, use YYYY-MM-DD");
                    Environment.Exit(1);
                }
                targetDateTime = parsedDate;
            }
            else
            {
                targetDateTime = DateTime.Now.Date;
            }

            if (!TimeSpan.TryParse(time, out var parsedTime))
            {
                Console.WriteLine("Invalid time format, use HH:MM:SS");
                Environment.Exit(1);
            }

            targetDateTime = targetDateTime.Add(parsedTime);
            var now = DateTime.Now;

            if (targetDateTime <= now)
            {
                Console.WriteLine("Target date/time must be in the future");
                Environment.Exit(1);
            }

            var totalSeconds = (int)(targetDateTime - now).TotalSeconds;

            for (int elapsed = 0; elapsed <= totalSeconds; elapsed++)
            {
                double percentage = (elapsed / (double)totalSeconds) * 100.0;
                Console.Write($"\rEndzeit: {targetDateTime} [" + new string('=', (int)(percentage / 2)) + new string(' ', 50 - (int)(percentage / 2)) + $"] {percentage:F2}%");
                Thread.Sleep(1000);
            }

            Console.WriteLine("\nEndzeit reached!");
        }, dateOption, timeOption);

        await rootCommand.InvokeAsync(args);
    }
}
