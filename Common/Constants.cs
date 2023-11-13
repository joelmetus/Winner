using System.Collections.Immutable;

namespace Common
{
    public static class Constants
    {
        public const int NumberOfColumns = 5;
        public const string DefaultFilePath = @"D:\Repos\Winner\race-results.txt";
        public static ImmutableList<string> Races = ImmutableList.Create(
            "sackRace",
            "1000m",
            "eggRace"
        );
    }
}
