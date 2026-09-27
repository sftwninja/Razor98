// tiledata/art index get loaded on the first item Razor sees, which froze
// the client ~3 s entering the world on a PII. load them in the background
// while the client starts instead. static ctors are thread safe, so if the
// UI thread gets there first it just waits.

using System;
using System.Threading;

namespace Assistant
{
    internal static class Win98Preload
    {
        private static bool s_Started;

        public static void Start()
        {
            if (s_Started)
                return;
            s_Started = true;

            Thread t = new Thread(Run);
            t.IsBackground = true;
            t.Name = "Win98Preload";
            t.Priority = ThreadPriority.BelowNormal;
            t.Start();
        }

        private static void Run()
        {
            // TileData looks at the art index for its format, so art first
            Load(delegate { Ultima.Art.IsUOAHS(); });
            Load(delegate { int n = Ultima.TileData.ItemTable.Length; });
            Load(delegate { Ultima.Hues.GetHue(0); });
        }

        private static void Load(ThreadStart load)
        {
            try
            {
                load();
            }
            catch
            {
                // will fail again on the UI thread, which handles it
            }
        }
    }
}
