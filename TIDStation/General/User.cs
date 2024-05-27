using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIDStation.General
{
    public static class User
    {
        static User()
        {
            if(!Directory.Exists(ConfigFolder))
                Directory.CreateDirectory(ConfigFolder);
        }
        public static string Documents => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string ConfigFolder => Path.Combine(Documents, "TIDStation");
        public static string ConfigFile => Path.Combine(ConfigFolder, "settings.conf");
    }
}
