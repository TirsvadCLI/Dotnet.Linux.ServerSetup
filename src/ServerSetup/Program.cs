using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.IO;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Serilog;
using Serilog.Context;

using YamlDotNet;
using YamlDotNet.Serialization;

using TirsvadCLI.Linux.LinuxServerSetup.Model;

namespace TirsvadCLI.Linux.ServerSetup;

static class Program
{
    private static bool _cmdOptionVerbose { get; set; }
    private static bool _cmdOptionNoUpgradeOs { get; set; }
    private static string _configurationFile = "conf/config.yaml";
    private static string _customServerSettingsPath = "conf/CustomServerSettings";
    private static ServerSetting? _customServerSettings { get; set; }
    private static Distributions? _Distributions { get; set; }
    private static string? _configDistribution = null;
    private static string? _configDistrobutionVersion = null;
    private static Version? _assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
    private const string _errorMsgMissingConfigurationFiles = @"
    Missing configuration files
    Please run ./ServerSetup config copy
    Then make the change in the default settings or keep the default server settings!
    ";

    static async Task Main(string[] args)
    {
        //_customServerSettingsPath = System.AppDomain.CurrentDomain.BaseDirectory + "conf/CustomServerSettings";
        Command cmd = new RootCommand
        {
            new Option<bool>(new [] {"--no-upgrade-os"}, description: "do not upgrade OS"),
            new Option<bool>(new[] { "--verbose", "-v" }, description: "Explain what is being done."),
            new Option<int>(new[] { "--log-level", "-l" }, description: "Log level 0 = Verbose, 1 = Debug, 2 = Information, 3 = Warning, 4 = Error, 5 = Fatal",
                getDefaultValue: () => 3),
            new Command("config", description: "Do some configuration before serverSetup")
            {
                // new Command("download", description: "Get config file via url")
                // {
                //     new Argument<string>("url", description: "Url for downloading configuration settings."),
                //     new Option<string>(new[] { "--user", "-u" }, description: "Username for access url"),
                //     new Option<string>(new[] { "--token", "-t" }, description: "User token for access url"),
                //     new Option<int>(new[] { "--strip-components", "-s" }, description: "Strip NUMBER leading components from tarbal file"),
                //     new Option<bool>(new[] { "--verbose", "-v" }, description: "Explain what is being done."),
                //     new Option<int>(new[] { "--log-level", "-l" }, description: "Log level 0 = Verbose, 1 = Debug, 2 = Information, 3 = Warning, 4 = Error, 5 = Fatal",
                //         getDefaultValue: () => 3),
                // }.WithHandler(nameof(CmdHandleConfigDownload)),

                new Command("copy", description: "Copy example of server settings to custom server settings")
                {
                    new Option<bool>(new[] { "--verbose", "-v" }, description: "Explain what is being done."),
                }.WithHandler(nameof(CmdHandleConfigCopy)),
            }
        }.WithHandler(nameof(Run));

        await cmd.InvokeAsync(args);
    }
    static Command WithHandler(this Command command, string name)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var method = typeof(Program).GetMethod(name, flags);

        var handler = CommandHandler.Create(method!);
        command.Handler = handler;
        return command;
    }

    // static async Task CmdHandleConfigDownload(
    //     string url,
    //     string user,
    //     string token,
    //     int stripComponents,
    //     bool verbose,
    //     int logLevel
    // )
    // {
    //     await _CreateLogger(verbose, logLevel);
    //     if (verbose && logLevel < 2)
    //     {
    //         Console.WriteLine($"Url {url}");
    //         Console.WriteLine($"user {user}");
    //         Console.WriteLine($"token {token}");
    //         Console.WriteLine($"stripComponents {stripComponents}");
    //         Console.WriteLine($"verbose {verbose}");
    //         Console.WriteLine($"logLevel {logLevel}");
    //     }
    // }

    static async Task CmdHandleConfigCopy(bool verbose, int logLevel)
    {
        await _CreateLogger(verbose, logLevel);
        Log.Information("Copying settings file");
        await Program.CopyDirectory(@"./conf/.DefaultServerSettings", _customServerSettingsPath, true);
    }

    static async Task Run(bool noUpgradeOs, bool verbose, int logLevel)
    {
        Log.Information($"Linux Server Configuration");

        var tasks = new List<Task>();
        tasks.Add(LoadConfigOsCompatibilities());
        tasks.Add(_CreateLogger(verbose, logLevel));
        await Task.WhenAll(tasks);

        await _PreCheck();

        tasks.Clear();

        #region "Check for custom server settings"
        int count = System.IO.Directory.EnumerateFiles(_customServerSettingsPath, "*.*").SkipWhile(name => name.EndsWith("README.md")).Count();
        if (count == 0)
        {
            Log.Error(_errorMsgMissingConfigurationFiles);
            System.Environment.Exit(1);
        }
        #endregion

        try
        {
            PackageManager.listOfAvaiblePackageManager.ForEach(i => Log.Debug("Avaible package manager {0}", i));
        }
        catch (Exception e)
        {
            Log.Warning("WARNING: {0}", string.Format(e.Message));
        }

        if (!noUpgradeOs)
        {
            tasks.Add(updateAndUpgradeOs());
        }

        using (StreamReader input = File.OpenText($"{_customServerSettingsPath}/settings.yaml"))
        {
            IDeserializer deserializer = new DeserializerBuilder().Build();
            _customServerSettings = deserializer.Deserialize<ServerSetting>(input);
        }

        await Task.WhenAll(tasks);
        await CreateGroups();
        await CreateUsers();

        /// <summary>
        /// This method will update and the upgrade OS
        /// <returns>
        /// Task.CompletedTask
        /// </returns>
        /// </summary>
        static Task updateAndUpgradeOs()
        {
            PackageManager.PmUpdate();
            PackageManager.PmUpgrade();
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method will create OS groups based on settings file found in <c>./conf/CustomServerSettings/settings.yaml</c>
        /// <returns>
        /// Task.CompletedTask
        /// </returns>
        /// </summary>
        static Task CreateGroups()
        {
            string? result;
            List<TirsvadCLI.Linux.LinuxServerSetup.Model.ServerSetting.SystemGroupAndUsers.Group>? groups = _customServerSettings?.systemGroupAndUsers?.groups?.ToList();
            foreach (var group in groups ?? Enumerable.Empty<TirsvadCLI.Linux.LinuxServerSetup.Model.ServerSetting.SystemGroupAndUsers.Group>())
            {
                if (group.name is not null && group.name != string.Empty)
                {
                    Process ps = new Process();
                    ps.StartInfo.UseShellExecute = false;
                    ps.StartInfo.RedirectStandardOutput = true;
                    ps.StartInfo.FileName = "groupadd";
                    ps.StartInfo.ArgumentList.Add(group.name);
                    ps.Start();
                    result = ps.StandardOutput.ReadToEnd();
                    ps.WaitForExit();
                    if (result != string.Empty)
                    {
                        Log.Warning($"User group something unexpected happend \n {result}");
                    }
                    else
                    {
                        Log.Debug($"User group {group.name} created");
                    }
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method will create OS users based on settings file found in <c>./conf/CustomServerSettings/settings.yaml</c>
        /// <returns>
        /// Task.CompletedTask
        /// </returns>
        /// </summary>
        static Task CreateUsers()
        {
            string? result;
            List<TirsvadCLI.Linux.LinuxServerSetup.Model.ServerSetting.SystemGroupAndUsers.User>? users = _customServerSettings?.systemGroupAndUsers?.users?.ToList();
            foreach (var user in users ?? Enumerable.Empty<TirsvadCLI.Linux.LinuxServerSetup.Model.ServerSetting.SystemGroupAndUsers.User>())
            {
                if (user.name is not null && user.name != "")
                {
                    Process ps = new Process();
                    ps.StartInfo.UseShellExecute = false;
                    ps.StartInfo.RedirectStandardOutput = true;
                    ps.StartInfo.FileName = "useradd";
                    ps.StartInfo.ArgumentList.Add("--create-home");
                    if (user.homeDirectory is not null && user.homeDirectory != "")
                    {
                        ps.StartInfo.ArgumentList.Add($"--home-dir {user.homeDirectory}");
                    }
                    if (user.groupMember is not null)
                    {
                        List<ServerSetting.SystemGroupAndUsers.Group> groups = user.groupMember.ToList();
                        if (groups is not null && groups.Any())
                        {
                            List<string> l = new List<string> { };
                            foreach (var group in groups)
                            {
                                if (group.name is not null)
                                    l.Add(group.name);
                            }
                            ps.StartInfo.ArgumentList.Add("--groups");
                            ps.StartInfo.ArgumentList.Add(string.Join(",", l));
                        }
                    }
                    if (user.password is not null && user.password != "")
                    {
                        ps.StartInfo.ArgumentList.Add("-p");
                        ps.StartInfo.ArgumentList.Add(user.password); // encrypted password passed in here
                    }
                    ps.StartInfo.ArgumentList.Add("--shell");
                    ps.StartInfo.ArgumentList.Add(user.defaultShell);
                    ps.StartInfo.ArgumentList.Add(user.name);
                    Log.Information("Added user with cmd: useradd {0}", string.Join(" ", ps.StartInfo.ArgumentList.ToList()));
                    ps.Start();
                    result = ps.StandardOutput.ReadToEnd();
                    ps.WaitForExit();
                    if (user.superUser)
                    {
                        ps = new Process();
                        ps.StartInfo.UseShellExecute = false;
                        ps.StartInfo.RedirectStandardOutput = true;
                        ps.StartInfo.FileName = "usermod";
                        ps.StartInfo.ArgumentList.Add("--append");
                        ps.StartInfo.ArgumentList.Add("--groups");
                        ps.StartInfo.ArgumentList.Add("sudo");
                        ps.StartInfo.ArgumentList.Add(user.name);
                        ps.Start();
                        result = ps.StandardOutput.ReadToEnd();
                        ps.WaitForExit();
                        Log.Information($"Added superuser for {user.name}");
                    }
                }
            }
            return Task.CompletedTask;
        }
    }

    static Task _CreateLogger(bool verbose, int logLevel)
    {
        if (verbose) _cmdOptionVerbose = true;
        Serilog.Events.LogEventLevel logLevelEvent = Serilog.Events.LogEventLevel.Information;
        switch (logLevel)
        {
            case 0:
                logLevelEvent = Serilog.Events.LogEventLevel.Verbose;
                break;
            case 1:
                logLevelEvent = Serilog.Events.LogEventLevel.Debug;
                break;
            case 2:
                logLevelEvent = Serilog.Events.LogEventLevel.Information;
                break;
            case 3:
                logLevelEvent = Serilog.Events.LogEventLevel.Warning;
                break;
            case 4:
                logLevelEvent = Serilog.Events.LogEventLevel.Error;
                break;
            case 5:
                logLevelEvent = Serilog.Events.LogEventLevel.Fatal;
                break;
        }
        if (verbose)
        {
            Log.Logger = new LoggerConfiguration()
                              .MinimumLevel.Debug()
                              .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}", restrictedToMinimumLevel: logLevelEvent)
                              .WriteTo.File("logs/run.log", outputTemplate: "[{Timestamp:HH:mm:ss} ({SourceContext}.{Method}) {Level:u3}] {Message:lj}{NewLine}{Exception}")
                              .CreateLogger();
        }
        else
        {
            Log.Logger = new LoggerConfiguration()
                              .MinimumLevel.Debug()
                              .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
                              .WriteTo.File("logs/run.log", outputTemplate: "[{Timestamp:HH:mm:ss} ({SourceContext}.{Method}) {Level:u3}] {Message:lj}{NewLine}{Exception}")
                              .CreateLogger();
        }
        Log.Debug($"Log level is {logLevel} equal to {logLevelEvent}");
        return Task.CompletedTask;
    }

    public static Task<string> ReadFileToString(string path)
    {
        return Task.Run(() =>
        {
            return File.ReadAllText(path);
        });
    }

    /*
      Loads this scripts OS compatibilities
    */
    public static Task LoadConfigOsCompatibilities()
    {
        using (StreamReader input = File.OpenText(_configurationFile))
        {
            DeserializerBuilder deserializerBuilder = new DeserializerBuilder();
            IDeserializer deserializer = deserializerBuilder
            .Build();
            _Distributions = deserializer.Deserialize<Distributions>(input);
        }
        return Task.CompletedTask;
    }

    /*
      Checking OS
      Checking user has root privilige
    */
    private static async Task _PreCheck()
    {
        try
        {
            await CheckOs();
        }
        catch (PlatformNotSupportedException e)
        {
            Console.WriteLine("WARNING: {0}", e.Message);
            Console.WriteLine("Exiting...");
            System.Environment.Exit(1);
        }
        try
        {
            await CheckRoot();
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine("WARNING: {0}", e.Message);
            Console.WriteLine("Exiting...");
            System.Environment.Exit(1);
        }

        static async Task CheckRoot()
        {
            char[] separators = { '\n', ',', '.', ' ' };
            string result = "";
            Process? ps = null;

            ps = new Process();
            ps.StartInfo.UseShellExecute = false;
            ps.StartInfo.RedirectStandardOutput = true;
            ps.StartInfo.FileName = "whoami";
            ps.Start();

            string userName = ps.StandardOutput.ReadToEnd().Trim();

            await ps.WaitForExitAsync();

            ps = new Process();
            ps.StartInfo.UseShellExecute = false;
            ps.StartInfo.RedirectStandardOutput = true;
            ps.StartInfo.FileName = "id";
            ps.StartInfo.ArgumentList.Add("-u");

            ps.Start();

            result = ps.StandardOutput.ReadToEnd();
            int userId = Int32.Parse(result);

            Log.Debug($"User ID of who is running this: {userId}");

            await ps.WaitForExitAsync();

            if (userId == 0)
            {
                return;
            }

            ps = new Process();
            ps.StartInfo.UseShellExecute = false;
            ps.StartInfo.RedirectStandardOutput = true;
            ps.StartInfo.FileName = "id";
            ps.StartInfo.ArgumentList.Add("-nG");
            ps.StartInfo.ArgumentList.Add(userName);
            ps.Start();

            result = ps.StandardOutput.ReadToEnd();
            ps.WaitForExit();

            if (result.Split(separators).Contains("sudo"))
            {
                throw new UnauthorizedAccessException("Use 'sudo' in front of this application as it need root privilige");
            }
            throw new UnauthorizedAccessException("User " + userName + " with Id " + userId + " is unauthorized");
        }

        static Task CheckOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                /*
                    Get linux distrobution and version
                */
                foreach (string line in File.ReadAllLines("/etc/os-release"))
                {
                    if (line.StartsWith("NAME"))
                    {
                        _configDistribution = line.Substring(5).Trim('"');
                    }
                    if (line.StartsWith("VERSION_ID"))
                    {
                        _configDistrobutionVersion = line.Substring(11).Trim('"');
                    }
                }
                int index = 0;
                for (int i = 0; i < _Distributions!.distributions!.Count; i++)
                {
                    if (_Distributions.distributions[i].distribution == _configDistribution)
                    {
                        index = i;
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                throw new PlatformNotSupportedException("OSX is not supported");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("Microsoft Windows is not supported");
            }
            else
            {
                throw new PlatformNotSupportedException("Cannot determine operating system!");
            }
            return Task.CompletedTask;

        }
    }

    /// <summary>
    /// This method will copy a Directory tree
    /// <returns>
    /// Task.CompletedTask
    /// </returns>
    /// </summary>
    /// <param name="sourceDir">the source directory</param>
    /// <param name="destinationDir">the destination directory</param>
    /// <param name="recursive">recursive copy</param>
    public static Task CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        Log.Debug($"Copy {sourceDir} to {destinationDir} with recursive {recursive}");
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            Log.Debug($"cp {destinationDir} {targetFilePath}");
            file.CopyTo(targetFilePath, true);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
        return Task.CompletedTask;
    }
}
