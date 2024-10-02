using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Vgreet.Analytics.GUI
{
    public class FileProcesser(IEnumerable<FileInfo> files, ProgressTask processTask, double fileIncrement)
    {
        private readonly YearlyAnalytics _yearlyAnalytics = [];
        private readonly IEnumerable<FileInfo> _files = files;
        private readonly ProgressTask _processTask = processTask;
        private readonly double _fileIncrement = fileIncrement;

        public async Task<YearlyAnalytics> Process()
        {
            YearlyAnalytics yearlyAnalytics = [];
            MonthlyAnalytics monthlyAnalytics = [];
            DailyAnalytics dailyAnalytics = [];

            foreach (var file in _files)
            {
                using var stream = file.OpenRead();
                var analytics = await JsonSerializer.DeserializeAsync<Analytic[]>(stream);

                if(analytics is null || analytics.Length == 0)
                {
                    _processTask.Increment(_fileIncrement);
                    continue;
                }

                var date = GetDateOnly(file);
                var hourly = ConvertToStats(analytics);

                if (!yearlyAnalytics.TryGetValue(date.Year, out var monthlyStats))
                {
                    monthlyStats = [];
                    yearlyAnalytics.Add(date.Year, monthlyStats);
                }

                if(!monthlyStats.TryGetValue(date.Month, out var dailyStats))
                {
                    dailyStats = [];
                    monthlyStats.Add(date.Month, dailyStats);
                }

                dailyStats.Add(date.Day, hourly);
                _processTask.Increment(_fileIncrement);
            }

            return yearlyAnalytics;
        }

        private DateOnly GetDateOnly(FileInfo file)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FullName);
            fileName = fileName.Replace("analytic", "");
            return DateOnly.ParseExact(fileName, "yyyyMd");
        }

        private HourlyAnalytics ConvertToStats(Analytic[] analytics)
        {
            HourlyAnalytics hourly = [];

            foreach(var analytic in analytics)
            {
                if (string.IsNullOrWhiteSpace(analytic.Event))
                {
                    continue;
                }

                var action = ParseAction(analytic);

                var hour = analytic.DateTime.Hour;

                if (hourly.TryGetValue(hour, out var eventsForHour))
                {
                    if (eventsForHour.TryGetValue(action, out var stats))
                    {
                        stats.Count += 1;
                    }
                    else
                    {
                        eventsForHour.Add(action, new Stats());
                    }
                }
                else
                {
                    eventsForHour = [];
                    hourly.Add(hour, eventsForHour);
                    eventsForHour.Add(action, new Stats());
                }
            }

            return hourly;
        }

        private string ParseAction(Analytic analytic)
        {
            var action = analytic.Event[..^2];
            return action.Trim('_');
        }
    }


    public class YearlyAnalytics : Dictionary<int, MonthlyAnalytics> { }

    public class MonthlyAnalytics : Dictionary<int, DailyAnalytics>{ }

    public class DailyAnalytics : Dictionary<int, HourlyAnalytics> { }

    public class HourlyAnalytics : Dictionary<int, EventAtHour> { }

    public class EventAtHour : Dictionary<string, Stats> { }

    public class Stats
    {
        public int Count { get; set; }

        public Stats()
        {
            Count = 1;
        }
    }
}
