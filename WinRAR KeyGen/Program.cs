using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Spectre.Console;

namespace WinRAR_KeyGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "WinRAR KeyGen | Eaminx Corporation.";
            AnsiConsole.Clear();

            var title = new FigletText(FigletFont.Default, "WINRAR")
                .Centered()
                .Color(Color.Cyan1);
            AnsiConsole.Write(title);

            AnsiConsole.Write(new Rule("[dim]── Registration Tool ──[/]").RuleStyle("dim").LeftJustified());

            var infoTable = new Table()
                .Border(TableBorder.None)
                .HideHeaders()
                .AddColumn(new TableColumn("").Width(12))
                .AddColumn(new TableColumn(""));

            infoTable.AddColumn(new TableColumn(""));
            infoTable.AddRow(
                "[dim]Version[/]",
                "[cyan]2.1.0[/]",
                $"[dim]{DateTime.Now:dd MMM yyyy}[/]");
            infoTable.AddRow(
                "[dim]User[/]",
                $"[white]{Environment.UserName}[/]",
                $"[dim]{DateTime.Now:HH:mm}[/]");

            AnsiConsole.Write(infoTable);
            AnsiConsole.WriteLine();

            string userName = PromptStyled("YOUR NAME", "Eaminx");
            string companyName = PromptStyled("LICENSE TYPE", "Unlimited Company License");
            if (string.IsNullOrWhiteSpace(companyName)) companyName = "Unlimited Company License";

            string winrarPath = GetWinRarPath();
            if (string.IsNullOrEmpty(winrarPath))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Panel("[red]✗ WinRAR installation not found[/]\n[dim]Searched registry and common paths[/]")
                    .Header("[red] Error [/]", Justify.Center)
                    .BorderColor(Color.Red));
                WaitForExit();
                return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]→ Found WinRAR at [white]{winrarPath}[/][/]");

            CheckAndCloseWinrar();

            string targetFile = Path.Combine(winrarPath, "rarreg.key");
            string appDataWinRAR = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinRAR");
            string appDataTarget = Path.Combine(appDataWinRAR, "rarreg.key");

            try
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Rule("[dim]Generating License[/]").RuleStyle("dim"));

                var keyContent = AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .Start("[cyan]Generating cryptographic key...[/]", ctx =>
                    {
                        Thread.Sleep(400);
                        ctx.Status("[cyan]Computing elliptic curve signatures...[/]");
                        Thread.Sleep(300);
                        ctx.Status("[cyan]Building registration data...[/]");
                        Thread.Sleep(200);

                        var info = WinRarKeygen.GenerateRegisterInfo(userName, companyName);
                        return WinRarKeygen.BuildRegFileContent(info);
                    });

                AnsiConsole.MarkupLine("[green]✓[/] License key generated");
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Rule("[dim]Injecting License[/]").RuleStyle("dim"));

                AnsiConsole.Progress()
                    .AutoClear(true)
                    .HideCompleted(false)
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn(Spinner.Known.Dots),
                    })
                    .Start(ctx =>
                    {
                        var task1 = ctx.AddTask("[cyan]Writing to WinRAR directory[/]", maxValue: 2);
                        var task2 = ctx.AddTask("[cyan]Writing to AppData[/]", maxValue: 2);

                        while (!ctx.IsFinished)
                        {
                            File.WriteAllText(targetFile, keyContent, Encoding.ASCII);
                            task1.Increment(2);
                            AnsiConsole.MarkupLine($"[green]✓[/] Written to [dim]{targetFile}[/]");

                            try
                            {
                                if (!Directory.Exists(appDataWinRAR))
                                    Directory.CreateDirectory(appDataWinRAR);
                                File.WriteAllText(appDataTarget, keyContent, Encoding.ASCII);
                                task2.Increment(2);
                                AnsiConsole.MarkupLine($"[green]✓[/] Written to [dim]{appDataTarget}[/]");
                            }
                            catch
                            {
                                task2.Increment(2);
                            }
                        }
                    });

                AnsiConsole.WriteLine();
                DrawSuccessPanel(userName, companyName);

                try
                {
                    string fullExePath = Path.Combine(winrarPath, "WinRAR.exe");
                    if (File.Exists(fullExePath))
                    {
                        Process.Start(new ProcessStartInfo(fullExePath) { UseShellExecute = true });
                        AnsiConsole.MarkupLine("[dim]→ Launched WinRAR[/]");
                    }
                }
                catch { }

                AnsiConsole.WriteLine();
                Countdown(5);
                Environment.Exit(0);
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Panel("[red]✗ Access Denied[/]\n[dim]Run this tool as Administrator[/]")
                    .Header("[red] Error [/]", Justify.Center)
                    .BorderColor(Color.Red));
                WaitForExit();
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Panel($"[red]✗ {ex.Message.EscapeMarkup()}[/]")
                    .Header("[red] Error [/]", Justify.Center)
                    .BorderColor(Color.Red));
                WaitForExit();
            }
        }

        static string PromptStyled(string label, string placeholder)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel("")
                .BorderColor(Color.Grey)
                .Header($"[cyan] {label} [/]", Justify.Left));

            AnsiConsole.Markup($"[dim]  Default: [white]{placeholder.EscapeMarkup()}[/][/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Markup("[cyan]  > [/]");

            string? input = Console.ReadLine();
            AnsiConsole.Write(new Rule("").RuleStyle("dim"));

            return string.IsNullOrWhiteSpace(input) ? placeholder : input.Trim();
        }

        static void DrawSuccessPanel(string user, string license)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Green)
                .HideHeaders()
                .AddColumn(new TableColumn("").Width(14))
                .AddColumn(new TableColumn(""));

            table.AddRow("[bold green]✓ SUCCESS[/]", "");
            table.AddRow("[dim]Licensed to[/]", $"[white]{user.EscapeMarkup()}[/]");
            table.AddRow("[dim]License[/]", $"[white]{license.EscapeMarkup()}[/]");
            table.AddRow("[dim]Status[/]", "[green]Activated[/]");

            var panel = new Panel(table)
                .Header("[bold green] ACTIVATION COMPLETE [/]", Justify.Center)
                .BorderColor(Color.Green)
                .Padding(1, 0, 1, 0);

            AnsiConsole.Write(panel);
        }

        static void Countdown(int seconds)
        {
            AnsiConsole.Progress()
                .AutoClear(true)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn { CompletedStyle = new Style(Color.Green) },
                    new SpinnerColumn(Spinner.Known.Dots),
                })
                .Start(ctx =>
                {
                    var task = ctx.AddTask("[dim]Closing[/]", maxValue: seconds * 10);
                    while (!ctx.IsFinished)
                    {
                        Thread.Sleep(100);
                        task.Increment(1);
                    }
                });
        }

        static void WaitForExit()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Markup("[dim]Press any key to exit...[/]");
            try { Console.ReadKey(); } catch { }
        }

        static string GetWinRarPath()
        {
            string[] registryPaths = {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WinRAR",
                @"HKEY_CURRENT_USER\Software\WinRAR"
            };

            foreach (var path in registryPaths)
            {
                object? val = Registry.GetValue(path, "exe64", null) ?? Registry.GetValue(path, "exe32", null);
                if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                {
                    string? dir = Path.GetDirectoryName(val.ToString());
                    if (dir != null && Directory.Exists(dir)) return dir;
                }
            }

            string[] uninstallKeys = {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinRAR archiver",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\WinRAR archiver",
                @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinRAR archiver"
            };

            foreach (var key in uninstallKeys)
            {
                object? val = Registry.GetValue(key, "InstallLocation", null) ?? Registry.GetValue(key, "UninstallString", null);
                if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                {
                    string path = val.ToString()!.Replace("\"", "").Trim();
                    if (File.Exists(path)) path = Path.GetDirectoryName(path) ?? path;
                    if (Directory.Exists(path)) return path;
                }
            }

            string[] commonPaths = {
                @"C:\Program Files\WinRAR",
                @"C:\Program Files (x86)\WinRAR",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinRAR"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinRAR")
            };

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "WinRAR.exe")))
                    return path;
            }

            return "";
        }

        static void CheckAndCloseWinrar()
        {
            Process[] processes = Process.GetProcessesByName("WinRAR");
            if (processes.Length > 0)
            {
                AnsiConsole.MarkupLine("[yellow]![/] WinRAR is running - closing...");
                foreach (var p in processes)
                {
                    p.Kill();
                    p.WaitForExit();
                }
                AnsiConsole.MarkupLine("[green]✓[/] WinRAR closed");
            }
        }
    }
}
