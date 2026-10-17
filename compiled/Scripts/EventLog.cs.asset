using System.Collections.Generic;
using System.Globalization;
using Unravel.Core;

namespace AnimationSample
{
    /// <summary>One animation event as the HUD's event feed lists it.</summary>
    public sealed class EventLogEntry
    {
        public string time = "";
        public string source = "";
        public string name = "";
        public string detail = "";
        public bool fresh;
    }

    /// <summary>
    /// The latest animation events around the hero, for the HUD's event feed. Characters add the events
    /// their scripts receive in OnAnimationEvent; events far from the hero are left out so the feed
    /// follows what the player looks at.
    /// </summary>
    [AutoStaticsCleanup]
    public static class EventLog
    {
        /// <summary>Lines the feed keeps.</summary>
        public const int Capacity = 8;
        /// <summary>Events further than this from the hero are not listed (m).</summary>
        public const float ListenRadius = 11.0f;

        private static readonly List<EventLogEntry> entries = new List<EventLogEntry>();

        /// <summary>Where the hero is; set by the hero every frame.</summary>
        public static Vector3 HeroPosition { get; set; }

        /// <summary>Bumps on every change, so the HUD refreshes only then.</summary>
        public static int Version { get; private set; }

        public static IReadOnlyList<EventLogEntry> Entries => entries;

        private static void OnStaticsCleanup()
        {
            entries.Clear();
            HeroPosition = Vector3.zero;
        }

        /// <summary>Lists an event a character at <paramref name="position"/> received.</summary>
        public static void Add(string source, AnimationEvent animationEvent, Vector3 position)
        {
            if (Vector3.Distance(position, HeroPosition) > ListenRadius)
            {
                return;
            }
            string detail = animationEvent.clipName ?? "";
            if (!string.IsNullOrEmpty(animationEvent.stringParameter))
            {
                detail = animationEvent.stringParameter + "  -  " + detail;
            }
            detail += "  (w " + animationEvent.weight.ToString("0.00", CultureInfo.InvariantCulture) + ")";
            foreach (var entry in entries)
            {
                entry.fresh = false;
            }
            entries.Insert(0, new EventLogEntry
            {
                time = Time.time.ToString("0.00", CultureInfo.InvariantCulture),
                source = source,
                name = animationEvent.name,
                detail = detail,
                fresh = true,
            });
            if (entries.Count > Capacity)
            {
                entries.RemoveAt(entries.Count - 1);
            }
            Version++;
        }
    }
}
