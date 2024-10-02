using Humanizer;
using Spectre.Console;
using System.Globalization;
using Vgreet.Analytics.GUI;

var validDirPath = AnsiConsole.Prompt(
    new TextPrompt<string>("Please provide the analytics directory:")
    .Validate(dirPath =>
    {
        var dir = new DirectoryInfo(dirPath);

        return dir.Exists ? ValidationResult.Success() : ValidationResult.Error("Directory does not exist");
    }));

var analyticsDir = new DirectoryInfo(validDirPath);

AnsiConsole.Clear();

var recursive = AnsiConsole.Prompt(
    new TextPrompt<bool>("Check directories recursively?")
    .AddChoice(true)
    .AddChoice(false)
    .DefaultValue(false)
    .WithConverter(choice => choice ? "y" : "n"));

AnsiConsole.Clear();

List<DirectoryInfo> dirsToRead = [analyticsDir];
double dirsIncrement = 100;
double filesIncrement = 100;

AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots2)
    .SpinnerStyle(Style.Parse("yellow bold"))
    .Start("Checking directory...", ctx =>
    {
        if (recursive)
        {
            AnsiConsole.MarkupLine("Looking for directories");
            dirsToRead.AddRange(analyticsDir.EnumerateDirectories("", SearchOption.AllDirectories));
        }

        dirsIncrement = 100 / dirsToRead.Count;

        var fileCount = 0;

        AnsiConsole.MarkupLine("Looking for files");
        var files = analyticsDir.EnumerateFiles("", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        fileCount += files.Count();

        filesIncrement = 100 / fileCount;
    });

AnsiConsole.Clear();

YearlyAnalytics yearlyAnalytics = [];

await AnsiConsole.Progress()
    .AutoClear(false)
    .HideCompleted(false)
    .Columns(
    [
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new RemainingTimeColumn(),
        new SpinnerColumn(),
    ])
    .StartAsync(async ctx =>
    {
        var dirTask = ctx.AddTask("[green]Reading directories[/]");
        var filesTask = ctx.AddTask("[green]Reading files[/]");
        var processingTask = ctx.AddTask("[yellow]Processing analytics[/]");

        var dirManager = new DirectoryManager(dirsToRead, dirTask, filesTask, processingTask, dirsIncrement, filesIncrement);
        yearlyAnalytics = await dirManager.Process();
    });

AnsiConsole.Clear();

bool choseYear = true;

while (choseYear)
{
    int year;

    if(yearlyAnalytics.Count > 1)
    {
        year = TerminalManager.GetYearByPrompt(yearlyAnalytics);
    }
    else if(yearlyAnalytics.Count == 1)
    {
        year = yearlyAnalytics.First().Key;
    }
    else
    {
        AnsiConsole.MarkupLine("There were no [underline red]Years[/] found");
        Console.Read();
        break;
    }

    choseYear = false;

    if (year is 0)
    {
        TerminalManager.ShowAllTimeTable(yearlyAnalytics);
        var choice = TerminalManager.NavigationPrompt("Year");
        choseYear = true;
        continue;
    }

    bool choseMonth = true;

    while (choseMonth)
    {
        var monthlyAnalytics = yearlyAnalytics[year];
        int month;

        if(monthlyAnalytics.Count > 1)
        {
            month = TerminalManager.GetMonthByPrompt(monthlyAnalytics, year);
        }
        else if(monthlyAnalytics.Count == 1)
        {
            month = monthlyAnalytics.First().Key;
        }
        else
        {
            AnsiConsole.MarkupLine("There were no [underline red]Months[/] found");
            Console.Read();
            break;
        }

        choseMonth = false;

        if (month is 0)
        {
            TerminalManager.ShowAllYearTable(monthlyAnalytics, year);
            var choice = TerminalManager.NavigationPrompt("Month", "Year");

            switch (choice)
            {
                case "Year":
                    choseYear = true;
                    choseMonth = false;
                    continue;
                case "Month":
                    choseYear = false;
                    choseMonth = true;
                    continue;
            }
        }

        bool choseDay = true;

        while (choseDay)
        {
            var dailyAnalytics = monthlyAnalytics[month];

            int day;

            if(dailyAnalytics.Count > 1)
            {
                day = TerminalManager.GetDayByPrompt(dailyAnalytics, year, month);
            }
            else if(dailyAnalytics.Count == 1)
            {
                day = dailyAnalytics.First().Key;
            }
            else
            {
                AnsiConsole.MarkupLine("There were no [underline red]Days[/] found");
                Console.Read();
                break;
            }

            choseDay = false;

            if (day is 0)
            {
                TerminalManager.ShowAllMonthTable(dailyAnalytics, year, month);
                var choice = TerminalManager.NavigationPrompt("Day", "Month", "Year");
                choseYear = false;
                choseMonth = false;
                choseDay = false;

                switch (choice)
                {
                    case "Year":
                        choseYear = true;
                        continue;
                    case "Month":
                        choseMonth = true;
                        continue;
                    case "Day":
                        choseDay = true;
                        continue;
                }
            }

            var hourlyAnalytics = dailyAnalytics[day];

            TerminalManager.ShowAllDayTable(hourlyAnalytics, year, month, day);
            var promptChoice = TerminalManager.NavigationPrompt("Hourly Graph", "Day", "Month", "Year");
            choseYear = false;
            choseMonth = false;
            choseDay = false;

            switch (promptChoice)
            {
                case "Year":
                    choseYear = true;

                    continue;
                case "Month":
                    choseMonth = true;
                    continue;
                case "Day":
                    choseDay = true;
                    continue;
                case "Hourly Graph":
                    var choseAction = true;

                    while (choseAction)
                    {
                        TerminalManager.ShowHourlyBarChart(hourlyAnalytics, year, month, day);
                        var choice = TerminalManager.NavigationPrompt("Event", "Year", "Month", "Day");
                        choseYear = false;
                        choseMonth = false;
                        choseDay = false;
                        choseAction = false;

                        switch (choice)
                        {
                            case "Year":
                                choseYear = true;
                                continue;
                            case "Month":
                                choseMonth = true;
                                continue;
                            case "Day":
                                choseDay = true;
                                continue;
                            case "Action":
                                choseAction = true;
                                continue;
                        }
                    }

                    break;
            }
        }
    }
}