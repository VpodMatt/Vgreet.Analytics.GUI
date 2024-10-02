using Humanizer;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Vgreet.Analytics.GUI
{
    public static class TerminalManager
    {
        public static int GetYearByPrompt(YearlyAnalytics yearlyAnalytics)
        {
            var year = AnsiConsole.Prompt(
                new SelectionPrompt<int>()
                .Title("Select year")
                .AddChoices([.. yearlyAnalytics.Keys, 0])
                .UseConverter(x => x == 0 ? "Display stats for all years" : x.ToString())
            );

            AnsiConsole.Clear();
            return year;
        }

        public static int GetMonthByPrompt(MonthlyAnalytics monthlyAnalytics, int year)
        {
            var month = AnsiConsole.Prompt(
                new SelectionPrompt<int>()
                .Title($"Select month : ##-##-{year}")
                .AddChoices([.. monthlyAnalytics.Keys, 0])
                .PageSize(13)
                .UseConverter(x => x switch
                    {
                        0 => "Display stats for entire year",
                        var months => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(months),
                }));

            AnsiConsole.Clear();
            return month;
        }

        public static int GetDayByPrompt(DailyAnalytics dailyAnalytics, int year, int month)
        {
            var day = AnsiConsole.Prompt(new SelectionPrompt<int>()
                .Title($"Select day : ##-{month}-{year}")
                .AddChoices([.. dailyAnalytics.Keys, 0])
                .PageSize(13)
                .UseConverter(x => x is 0 ? "Display stats for entire month" : x.ToString()));

            AnsiConsole.Clear();
            return day;
        }

        public static string GetEventByPrompt(HourlyAnalytics hourlyAnalytics, int year, int month, int day)
        {
            var events = hourlyAnalytics.Values.SelectMany(x => x.Keys.ToArray()).Distinct();

            var action = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title($"Choose event : {day}-{month}-{year}")
            .AddChoices(events)
            .MoreChoicesText("[grey]Move up and down to reveal more events[/]"));

            AnsiConsole.Clear();
            return action;
        }

        public static void ShowAllTimeTable(YearlyAnalytics yearlyAnalytics)
        {
            var allTimeTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("Analytics for all time")
                .Caption("Analytics are for how much a given event has occured in the specified timeframe")
                .AddColumn("Event")
                .AddColumn("Count");

            Dictionary<string, int> actionCounts = [];
            SetCountsForAllTime(actionCounts, yearlyAnalytics);
            var orderedCount = actionCounts.OrderBy(x => x.Key);

            foreach (var actionCount in orderedCount)
            {
                allTimeTable.AddRow(actionCount.Key.ToString(), actionCount.Value.ToString());
            }

            AnsiConsole.Write(allTimeTable);
        }

        public static void ShowAllYearTable(MonthlyAnalytics monthlyAnalytics, int year)
        {
            var yearTable = new Table()
                .Border(TableBorder.Rounded)
                .Title($"Analytics for the year {year}")
                .Caption("Analytics are for how much a given event has occured in the specified timeframe")
                .AddColumn("Event")
                .AddColumn("Count");

            Dictionary<string, int>  actionCounts = [];

            SetCountsForYear(actionCounts, monthlyAnalytics);

            var orderedCount = actionCounts.OrderBy(x => x.Key);

            foreach (var actionCount in orderedCount)
            {
                yearTable.AddRow(actionCount.Key.ToString(), actionCount.Value.ToString());
            }

            AnsiConsole.Write(yearTable);
        }

        public static void ShowAllMonthTable(DailyAnalytics dailyAnalytics, int year, int month)
        {
            var monthTable = new Table()
                .Border(TableBorder.Rounded)
                .Title($"Analytics for {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} of {year}")
                .Caption("Analytics are for how much a given event has occured in the specified timeframe")
                .AddColumn("Event")
                .AddColumn("Count");

            Dictionary<string, int> actionCounts = [];
            SetCountsForMonth(actionCounts, dailyAnalytics);
            var orderedCount = actionCounts.OrderByDescending(x => x.Key);

            foreach (var actionCount in orderedCount)
            {
                monthTable.AddRow(actionCount.Key.ToString(), actionCount.Value.ToString());
            }

            AnsiConsole.Write(monthTable);
        }

        public static void ShowAllDayTable(HourlyAnalytics hourlyAnalytics, int year, int month, int day)
        {
            var dayTable = new Table()
                .Border(TableBorder.Rounded)
                .Title($"Analytics for {day.ToOrdinalWords()} of {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} of {year}")
                .Caption("Analytics are for how much a given event has occured in the specified timeframe")
                .AddColumn("Event")
                .AddColumn("Count");

            Dictionary<string, int> actionCounts = [];
            SetCountsForDay(actionCounts, hourlyAnalytics);
            var orderedCount = actionCounts.OrderByDescending(x => x.Key);

            foreach (var actionCount in orderedCount)
            {
                dayTable.AddRow(actionCount.Key.ToString(), actionCount.Value.ToString());
            }

            AnsiConsole.Write(dayTable);
        }

        public static void ShowHourlyBarChart(HourlyAnalytics hourlyAnalytics, int year, int month, int day)
        {
            var action = GetEventByPrompt(hourlyAnalytics, year, month, day);

            Dictionary<int, int> hourToActionCount = [];

            foreach (var hour in hourlyAnalytics)
            {
                var count = 0;

                if (hour.Value.TryGetValue(action, out var stat))
                {
                    count = stat.Count;
                }

                hourToActionCount.Add(hour.Key, count);
            }
            var max = hourToActionCount.Max(x => x.Value);
            var barChart = new BarChart()
                .Width(max < 60 ? 60 : max)
                .Label($"[LightSkyBlue1 bold underline]Action ({action}) occurance per hour[/] for {day.ToOrdinalWords()} of {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} of {year}");

            int index = 0;

            foreach (var hourActionCount in hourToActionCount)
            {
                index++;
                bool isEven = int.IsEvenInteger(index);
                barChart.AddItem(hourActionCount.Key.ToString() + ":00", hourActionCount.Value, isEven ? Color.NavajoWhite1 : Color.LightSkyBlue1);
            }

            AnsiConsole.Write(barChart);
        }

        public static string NavigationPrompt(params string[] args)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Navigation:")
            .AddChoices(args));

            AnsiConsole.Clear();
            return choice;
        }

        private static void SetCountsForAllTime(Dictionary<string, int> actionCounts, YearlyAnalytics yearlyAnalytics)
        {
            foreach(var monthlyAnalytics in yearlyAnalytics)
            {
                SetCountsForYear(actionCounts, monthlyAnalytics.Value);
            }
        }

        private static void SetCountsForYear(Dictionary<string, int> actionCounts, MonthlyAnalytics monthlyAnalytics)
        {
            foreach(var dailyAnalytics in monthlyAnalytics)
            {
                SetCountsForMonth(actionCounts, dailyAnalytics.Value);
            }
        }

        private static void SetCountsForMonth(Dictionary<string, int> actionCounts, DailyAnalytics dailyAnalytics)
        {
            foreach(var hourlyAnalytics in dailyAnalytics)
            {
                SetCountsForDay(actionCounts, hourlyAnalytics.Value);
            }
        }

        private static void SetCountsForDay(Dictionary<string, int> actionCounts, HourlyAnalytics hourlyAnalytics)
        {
            foreach (var action in hourlyAnalytics)
            {
                SetCountForHour(actionCounts, action.Value);
            }
        }

        private static void SetCountForHour(Dictionary<string, int> actionCounts, EventAtHour action)
        {
            foreach(var stats in action)
            {
                SetActionCount(actionCounts, stats.Key);
            }
        }

        private static void SetActionCount(Dictionary<string, int> actionCounts, string actionName)
        {
            if (actionCounts.TryGetValue(actionName, out var count))
            {
                actionCounts[actionName] += 1;
            }
            else
            {
                actionCounts.Add(actionName, 1);
            }
        }
    }
}
