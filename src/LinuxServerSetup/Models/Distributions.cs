using System;
using System.Collections.Generic;

using YamlDotNet.Serialization;

namespace TirsvadCLI.Linux.LinuxServerSetup;

public class Distributions
{
  [YamlMember(Alias = "Distributions")]
  public List<Distribution> distributions { get; set; }
  public class Version
  {
    public string version { get; set; }
    public List<string> Files { get; set; }
  }

  public class Distribution
  {
    public string distribution { get; set; }
    public List<Version> Versions { get; set; }
  }
}
