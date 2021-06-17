#region ================= Namespaces

using System.IO;
using System.Threading.Tasks;
using mxd.SQL2.Data;

#endregion

namespace mxd.SQL2.Games
{
    public static class SpecialActions
    {
        #region ================= Properties

        static Task currentWait;

        #endregion

        #region ================= Methods

        public static void DisableQuakeRC()
        {
            string[] myFiles = Directory.GetFiles(GameHandler.Current.GamePath + "\\" + Configuration.Mod, "*.rc", SearchOption.AllDirectories);

            foreach (string file in myFiles)
            {
                string newFile = file.Replace(".rc", ".rc_ignore");
                File.Move(file, newFile);
            }

            /// TODO: Part below can be written smarter. Check and cancel Tasks properly...
            if (currentWait == null)
            {
                currentWait = WaitAndBringBackRC();
                return;
            }

            if (currentWait.Status == TaskStatus.RanToCompletion ||
                currentWait.Status == TaskStatus.Canceled ||
                currentWait.Status == TaskStatus.Faulted)
            {
                currentWait.Dispose();
                currentWait = WaitAndBringBackRC();
                currentWait.Start();
            }
        }

        public static void EnableQuakeRC()
        {
            string[] myFiles = Directory.GetFiles(GameHandler.Current.GamePath + "\\" + Configuration.Mod, "*.rc_ignore", SearchOption.AllDirectories);

            foreach (string file in myFiles)
            {
                string newFile = file.Replace(".rc_ignore", ".rc");
                File.Move(file, newFile);
            }
        }

        private static async Task WaitAndBringBackRC()
        {
            /// Wait 10 sec before renaming quake.rc_ignore back to quake.rc
            await Task.Delay(10000);
            EnableQuakeRC();
        }

        #endregion
    }
}
