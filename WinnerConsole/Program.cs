using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Common;
using static Common.Utils;

namespace WinnerConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                var resultEntries = await ImportResultsAsync(logger);

                var cleanResultEntries = resultEntries != null && resultEntries.Count > 0
                    ? RemoveDiscrepancies(logger, resultEntries)
                    : null;

                var finalResults = cleanResultEntries != null && cleanResultEntries.Count > 0
                    ? CalculateFinalResult(logger, cleanResultEntries)
                    : null;

                if (finalResults != null)
                {
                    PrintWinners(logger, finalResults);
                }
                else
                {
                    logger.LogInformation("There are no valid entries.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during data processing.");
            }

            stopwatch.Stop();
            logger.LogInformation("Execution Time: {stopwatch.ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
        }
    }
}