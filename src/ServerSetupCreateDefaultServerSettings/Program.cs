using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using TirsvadCLI.Linux.LinuxServerSetup.Model;

namespace TirsvadCLI.Linux.ServerSetupCreateDefaultServerSettings;

class Program
{
    static Task Main(string[] args)
    {
        ServerSetting _serverSettings = new ServerSetting()
        {
            systemGroupAndUsers = new ServerSetting.SystemGroupAndUsers()
            {
                groups = new List<ServerSetting.SystemGroupAndUsers.Group>(){
                    new ServerSetting.SystemGroupAndUsers.Group() { name ="staff" },
                    new ServerSetting.SystemGroupAndUsers.Group() { name ="website1" },
                    new ServerSetting.SystemGroupAndUsers.Group() { name ="website2" }
                },
                users = new List<ServerSetting.SystemGroupAndUsers.User>() {
                    new ServerSetting.SystemGroupAndUsers.User() { name = "userwithsudo", password = "saHW9GdxihkGQ", superUser = true },
                    new ServerSetting.SystemGroupAndUsers.User() { name = "userWebsite1", password = "saHW9GdxihkGQ" },
                    new ServerSetting.SystemGroupAndUsers.User() { name = "userWebsite2", password = "saHW9GdxihkGQ", groupMember = new List<ServerSetting.SystemGroupAndUsers.Group>() {
                        new ServerSetting.SystemGroupAndUsers.Group() { name = "website2" }
                    }}
                },
            },
        };

        string stringBuilder = DumpAsYaml(_serverSettings).ToString();

        Stream stream = new MemoryStream();
        byte[] byteArray = Encoding.UTF8.GetBytes(stringBuilder);
        stream.Write(byteArray, 0, byteArray.Length);
        stream.Position = 0;
        using(Stream outStream = File.OpenWrite("conf/default.yaml"))
        {
            stream.CopyTo(outStream);
        }
        return Task.CompletedTask;
    }

    private static StringBuilder DumpAsYaml(object data)
    {
        Console.WriteLine("***Dumping Object Using Yaml Serializer***");
        var stringBuilder = new StringBuilder();
        var serializer = new Serializer();
        stringBuilder.AppendLine(serializer.Serialize(data));
        Console.WriteLine(stringBuilder);
        Console.WriteLine("");
        return(stringBuilder);
    }
}
