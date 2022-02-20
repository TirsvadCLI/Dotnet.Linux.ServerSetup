using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using CommandLine;
using TirsvadCLI.Linux;
using Serilog;
using YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TirsvadCLI.Linux.LinuxServerSetup;

internal class Program
{
  private static string _configurationFile = "conf/config.yaml";

  private static Distributions _Distributions = null;
  private static string _configDistribution = null;
  private static string _configDistrobutionVersion = null;

  public static void Main(string[] args)
  {
    Parser.Default.ParseArguments<CmdLineOptions>(args)
      .WithParsed(_CmdParserDoOptions);
      // .WithNotParsed(HandleParseError);

    Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

    Log.Information($"Linux Server Configuration {assemblyVersion}");

    LoadConfigOsCompatibilities();

    _PreCheck();

    Log.Debug("PM " + PackageManager.packageManager);

    try
    {
      PackageManager.listOfAvaiblePackageManager.ForEach(i => Log.Debug("Avaible package manager {0}", i));
      // for (int i = 0; i < PackageManager.listOfAvaiblePackageManager.Count; i++)
      // {
      //   Log.Debug("Avaible package manager {0}", PackageManager.listOfAvaiblePackageManager[i]);
      // }
    }
    catch (Exception e)
    {
      Log.Warning("WARNING: {0}", string.Format(e.Message));
    }
  }

  private static void _CmdParserDoOptions(CmdLineOptions options)
  {
    // if (options.Url is not null and not "")
    // {
    //   _customConfigFileUrl = options.Url;
    //   if (options.User is not null and not "")
    //   {
    //     if (options.Verbose) Console.WriteLine("User: " + options.User.ToString());
    //   }
    //   if (options.Token is not null and not "")
    //   {
    //     if (options.Verbose) Console.WriteLine("Token: " + options.Token.ToString());
    //   }
    //   if (options.StripComponents.HasValue)
    //   {
    //     if (options.Verbose) Console.WriteLine("User: " + options.StripComponents.ToString());
    //   }
    //   if (options.Verbose) Console.WriteLine("Donwload custom configuration file from: " + _customConfigFileUrl);
    // }
    if (options.Verbose)
    {
      // Console.WriteLine("Verbose : " + options.Verbose.ToString());
      Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug
                        )
                        .WriteTo.File("logs/run.log")
                        .CreateLogger();

    }
    else
    {
      Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console(
                            outputTemplate: "{Message:lj}{NewLine}{Exception}",
                            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
                        )
                        .WriteTo.File(
                          "logs/run.log",
                          outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                        )
                        .CreateLogger();
    }

    // List<Task> listOfTasks = new List<Task>();

    // foreach (var option in options.GetType().GetProperties())
    // {
    //     listOfTasks.Add(_parseOption(option.ToString()));
    // }

    // await Task.WhenAll(listOfTasks);

    // static void _parseOption(string option)
    // {

    //     return Task.CompletedTask;
    // }
  }

  private static void _CmdParserHandleParseError(IEnumerable<Error> errs)
  {
    //handle errors
  }

  // public static Task<string> ReadFileToText(string path)
  // {
  //   return Task.Run(() =>
  //   {
  //     return File.ReadAllText(path);
  //   });
  // }

  // Loads this scripts OS compatibilities to _Distributions
  public static void LoadConfigOsCompatibilities()
  {
    using (StreamReader input = File.OpenText(_configurationFile))
    {
      DeserializerBuilder deserializerBuilder = new DeserializerBuilder();
      IDeserializer deserializer = deserializerBuilder
      .Build();
      _Distributions = deserializer.Deserialize<Distributions>(input);
    }
  }

  private static void _PreCheck()
  {
    try
    {
      CheckOs();
    }
    catch (PlatformNotSupportedException e)
    {
      Console.WriteLine("WARNING: {0}", e.Message);
      Console.WriteLine("Exiting...");
      System.Environment.Exit(1);
    }
    try
    {
      CheckRoot();
    }
    catch (UnauthorizedAccessException e)
    {
      Console.WriteLine("WARNING: {0}", e.Message);
      Console.WriteLine("Exiting...");
      System.Environment.Exit(1);
    }

    static void CheckRoot()
    {
      char[] separators = { '\n', ',', '.', ' ' };
      string result = "";
      Process ps = null;

      ps = new Process();
      ps.StartInfo.UseShellExecute = false;
      ps.StartInfo.RedirectStandardOutput = true;
      ps.StartInfo.FileName = "whoami";
      ps.Start();

      string userName = ps.StandardOutput.ReadToEnd().Trim();

      ps.WaitForExit();

      ps = new Process();
      ps.StartInfo.UseShellExecute = false;
      ps.StartInfo.RedirectStandardOutput = true;
      ps.StartInfo.FileName = "id";
      ps.StartInfo.ArgumentList.Add("-u");

      ps.Start();

      result = ps.StandardOutput.ReadToEnd();
      int userId = Int32.Parse(result);

      Log.Debug($"User ID of who is running this: {userId}");

      ps.WaitForExit();

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

    static bool CheckOs()
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
        for (int i = 0; i < _Distributions.distributions.Count; i++)
        {
          if (_Distributions.distributions[i].distribution == _configDistribution)
          {
            index = i;
          }
        }
        return true;
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        throw new PlatformNotSupportedException("OSX is not supported");
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        throw new PlatformNotSupportedException("Microsoft Windows is not supported");
      }
      throw new PlatformNotSupportedException("Cannot determine operating system!");
    }
  }
}
