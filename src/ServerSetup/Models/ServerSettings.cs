using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace TirsvadCLI.Linux.LinuxServerSetup.Model;

public class ServerSetting
{
    public SystemGroupAndUsers systemGroupAndUsers = new SystemGroupAndUsers();
    public class SystemGroupAndUsers
    {
        public List<Group>? groups { get; set; }
        public List<User>? users { get; set; }

        public class Group
        {
            // public short? groupId { get; set; }
            public string? name { get; set; }
        }

        public class User
        {
            // public short? userId { get; set; }
            public string? name { get; set; }
            public string? password { get; set; }
            public List<Group>? groupMember { get; set; }
            public string? homeDirectory { get; set; }
            public string defaultShell { get; set; }
            public bool superUser { get; set; }

            public User()
            {
                defaultShell = "/bin/bash";
                superUser = false;
            }
        }
    }
}
