using com.tcs.tools.adminwindow.Core.Attributes;

namespace TCS.Admin.TCSTools.Admin.Editor.Sample
{
    public static class ExampleCommands
    {
        [AdminCommand("echo", help:"Echo back a string")]
        public static string Echo(string text, int times = 1)
        {
            var s = "";
            for (int i=0;i<times;i++) s += text + (i<times-1 ? " " : "");
            return s;
        }

        // Instance command example (will try DI, then scene)
        public class GameLoopRunner
        {
            [AdminCommand("start-run", category:"Gameplay", help:"Starts a run")]
            public static void StartRun() { UnityEngine.Debug.Log("Run started."); }
        }
    }
}