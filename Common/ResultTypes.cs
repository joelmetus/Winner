using System.Diagnostics;

namespace Common
{
    public class RaceResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly FinishTime { get; set; }
        public string Race { get; set; }

        public RaceResult(string name, int id, TimeOnly startTime, TimeOnly finishTime, string race)
        {
            Id = id;
            Name = name;
            StartTime = startTime;
            FinishTime = finishTime;
            Race = race;
        }

        public override string ToString() => $"{Id} - {Name} - {StartTime:HH:mm:ss} - {FinishTime.ToString("HH:mm:ss")} - {Race}";

        public TimeSpan GetDuration() => FinishTime.ToTimeSpan() - StartTime.ToTimeSpan();
    }

    public class FinalResult
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required HashSet<string> Races { get; set; }
        public required List<string> MissingRaces { get; set; }
        public required int RaceCount { get; set; }
        public required TimeSpan TotalTime { get; set; }
        public bool IsQualified { get; set; }
        
        public override string ToString() => $"Name: {Name} - ID: {Id} - Total Time: {TotalTime}";
    }
}
