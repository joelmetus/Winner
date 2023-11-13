using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Common
{
    public static class Utils
    {
        public static async Task<List<RaceResult>> ImportResultsAsync(ILogger logger, string filePath = Constants.DefaultFilePath)
        {
            logger.LogInformation("Starting import of results from: {filePath}", filePath);

            List<RaceResult> results = new();
            int lineNumber = 0;

            try
            {
                using StreamReader reader = new(filePath);
                string? line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        RaceResult result = ParseColumns(logger, line, lineNumber);
                        results.Add(result);
                    }
                    catch (FormatException formatException)
                    {
                        logger.LogWarning("{formatException.Message}", formatException.Message);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("{ex.Message}", ex.Message);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                logger.LogCritical("{ex.Message}", ex.Message);
            }
            catch (IOException ex)
            {
                logger.LogCritical("{ex.Message}", ex.Message);
            }

            return results;
        }

        public static List<RaceResult> ConvertResultsAsync(ILogger logger, List<string> rowList)
        {
            logger.LogInformation("Starting conversion of results from request body.");

            List<RaceResult> results = new();
            int lineNumber = 0;

            try
            {
                foreach (string row in rowList)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(row))
                        continue;

                    try
                    {
                        RaceResult result = ParseColumns(logger, row, lineNumber);
                        results.Add(result);
                    }
                    catch (FormatException formatException)
                    {
                        logger.LogWarning("{formatException.Message}", formatException.Message);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("{ex.Message}", ex.Message);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                logger.LogCritical("{ex.Message}", ex.Message);
            }
            catch (IOException ex)
            {
                logger.LogCritical("{ex.Message}", ex.Message);
            }

            return results;
        }

        private static RaceResult ParseColumns(ILogger logger, string line, int lineNumber)
        {
            List<string> errorMessages = new();
            List<string> splitLine = line.Split(',')
                .Select(x => x.Trim())
                .ToList();

            if (splitLine.Count != Constants.NumberOfColumns)
                throw new FormatException($"Line {lineNumber}: " + $"Invalid number of columns. Expected '{Constants.NumberOfColumns}' but found '{splitLine.Count}'.");

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            string name = textInfo.ToTitleCase(splitLine[0].ToLower());

            string race = splitLine[4];

            if (!RegEx.NameRegex().IsMatch(name))
                errorMessages.Add($"Invalid format for Name: '{name}'.");

            if (!Constants.Races.Contains(race))
                errorMessages.Add($"Invalid race name: '{race}'.");

            if (!int.TryParse(splitLine[1], out int id) || id <= 0)
                errorMessages.Add($"Invalid format for ID: '{splitLine[1]}'.");

            if (!TimeOnly.TryParseExact(splitLine[2], "HH:mm:ss", out TimeOnly startTime))
                errorMessages.Add($"Invalid format for Start Time: '{splitLine[2]}'.");

            if (!TimeOnly.TryParseExact(splitLine[3], "HH:mm:ss", out TimeOnly finishTime))
                errorMessages.Add($"Invalid format for Finish Time: '{splitLine[3]}'.");

            if (startTime != default && finishTime != default && startTime > finishTime)
                errorMessages.Add($"Start Time is after Finish Time.");

            if (errorMessages.Any())
            {
                throw new FormatException($"Line {lineNumber}: " + string.Join(" ", errorMessages));
            }

            return new RaceResult(
                name: name,
                id: id,
                startTime: startTime,
                finishTime: finishTime,
                race: race
            );
        }

        public static List<FinalResult> CalculateFinalResult(ILogger logger, List<RaceResult> results)
        {
            return results
                .GroupBy(p => new { p.Id, p.Name })
                .Select(group => new FinalResult
                {
                    Id = group.Key.Id,
                    Name = group.Key.Name,
                    TotalTime = group.Aggregate(TimeSpan.Zero, (sum, next) => sum + next.GetDuration()),
                    RaceCount = group.Count(),
                    Races = group.Select(r => r.Race).ToHashSet(),
                    MissingRaces = Constants.Races.Except(group.Select(r => r.Race)).ToList(),
                    IsQualified = group.Count() == Constants.Races.Count && group.Select(r => r.Race).ToHashSet().SetEquals(Constants.Races)
                })
                .ToList();
        }

        public static List<RaceResult> RemoveDiscrepancies(ILogger logger, List<RaceResult> results)
        {
            // Identify IDs with multiple names
            HashSet<int> idsWithMultipleNames = results
                .GroupBy(result => result.Id)
                .Where(group => group.Select(result => result.Name).Distinct().Count() > 1)
                .Select(group => group.Key)
                .ToHashSet();

            if (idsWithMultipleNames.Any())
            {
                logger.LogWarning("{message}", "Some IDs are associated with multiple names. The entries will be removed: " +
                    string.Join(", ", idsWithMultipleNames));
            }

            // Identify Names with multiple IDs
            HashSet<string> namesWithMultipleIds = results
                .GroupBy(result => result.Name)
                .Where(group => group.Select(result => result.Id).Distinct().Count() > 1)
                .Select(group => group.Key)
                .ToHashSet();

            if (namesWithMultipleIds.Any())
            {
                logger.LogWarning("{message}", "Some Names are associated with multiple IDs. The entries will be removed: " +
                    string.Join(", ", namesWithMultipleIds));
            }

            // Identify persons that participated in the same race multiple times
            HashSet<int> multipleParticipations = results
                .GroupBy(result => new { result.Id, result.Race })
                .Where(group => group.Count() > 1)
                .Select(group => group.Key.Id)
                .ToHashSet();

            if (multipleParticipations.Any())
            {
                logger.LogWarning("{message}", "Some people have participated in more than once in a race. The entries will be removed: " +
                    string.Join(", ", multipleParticipations));
            }

            // Filter out all discrepancies
            List<RaceResult> filteredResults = results
                .Where(result => !idsWithMultipleNames.Contains(result.Id) &&
                                 !namesWithMultipleIds.Contains(result.Name) &&
                                 !multipleParticipations.Contains(result.Id))
                .ToList();

            return filteredResults;

        }

        public static List<FinalResult>? FindWinners(ILogger logger, List<FinalResult> finalResults)
        {
            TimeSpan minTime = finalResults
                .Where(f => f.IsQualified)
                .Select(f => f.TotalTime)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Min();

            if (minTime == TimeSpan.Zero)
                return null;

            List<FinalResult> winners = finalResults.Where(f => f.TotalTime == minTime).ToList();

            return winners;
        }

        public static void PrintWinners(ILogger logger, List<FinalResult> finalResults)
        {
            TimeSpan minTime = finalResults
                .Where(f => f.IsQualified)
                .Select(f => f.TotalTime)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Min();

            if (minTime == TimeSpan.Zero)
            {
                Console.WriteLine("There are no qualified winners.");
                return;
            }
            else
            {
                List<FinalResult> winners = finalResults.Where(f => f.TotalTime == minTime).ToList();

                foreach (FinalResult winner in winners)
                    Console.WriteLine($"\n - The winner is: {winner?.Id} - {winner?.Name} - {winner?.TotalTime} - ");
            }
        }
    }
}
