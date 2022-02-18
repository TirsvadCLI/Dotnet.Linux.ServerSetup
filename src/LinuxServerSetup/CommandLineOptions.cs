using System;
using System.Collections.Generic;

using CommandLine;

namespace TirsvadCLI.Linux.LinuxServerSetup;

public class CmdLineOptions
{
    // [Option(longName: "user", Required = false, HelpText = "User for access download of configuration files")]
    // public string User { get; set; }

    // [Option(longName: "token", Required = false, HelpText = "User token for access download of configuration files")]
    // public string Token { get; set; }

    // [Option(shortName: 'u', longName: "url", Required = false, HelpText = "The url path to configuration files", Default = "")]
    // public string Url { get; set; }

    // [Option(shortName: 's', longName: "strip-components", Required = false, HelpText = "Strip NUMBER leading components from tarbal file")]
    // public Nullable<int> StripComponents { get; set; }

    // [Option(shortName: 'l', longName: "log", Required = false, HelpText = "Set the logging level")]
    // public bool Log { get; set; }

    [Option(shortName: 'v', longName: "verbose", Required = false, HelpText = "Set output to verbose messages.")]
    public bool Verbose { get; set; }

    // [Option('t', Separator = ':')]
    // public IEnumerable<string> Types { get; set; }
}
