#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Threading;
using osu.Game.Overlays.Mods;
using osu.Game.Screens.Select;

namespace osu.Game.BellaFiora
{
    public class Triggers
    {
        private static Server server = null!;
        private static bool footerButtonModsLoadedClicked = false;
        private static readonly List<Action<Server>> pending_actions = new List<Action<Server>>();

        public static void AssignToServer(Action<Server> action)
        {
            if (server == null)
                pending_actions.Add(s => action(s));
            else
                action(server);
        }

        public static void CarouselBeatmapsTrulyLoaded(SongSelect songSelect)
        {
            if (server == null && SynchronizationContext.Current != null)
            {
                server = new Server(SynchronizationContext.Current) { SongSelect = songSelect };
                foreach (var action in pending_actions)
                    action(server);
                pending_actions.Clear();
                server.Start();
            }
        }

        public static void FooterButtonModsLoaded(UserModSelectOverlay overlay)
        {
            Console.WriteLine("FooterButtonModsLoaded called");
            if (!footerButtonModsLoadedClicked)
            {
                // this will trigger the creation of all mod panels
                Console.WriteLine("FooterButtonModsLoaded called, showing the overlay.");
                // btn.TriggerClick();
                overlay.Show();
                footerButtonModsLoadedClicked = true;
            }
        }
    }
}
