using System.IO;

namespace com.tcs.tools.adminwindow.Core.Persistence
{
    public static class AdminPaths
    {
        public static string UserDir
        {
            get
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "UserSettings", "TCS.Admin");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }
        public static string HistoryFile => Path.Combine(UserDir, "history.json");
    }
}