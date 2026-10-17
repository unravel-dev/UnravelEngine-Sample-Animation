using System.Collections.Generic;
using Unravel.Core;

namespace AnimationSample
{
    /// <summary>Every station of the gallery, for the HUD and the hero's look-at.</summary>
    [AutoStaticsCleanup]
    public static class StationRegistry
    {
        private static readonly List<Station> stations = new List<Station>();

        public static IReadOnlyList<Station> All => stations;

        private static void OnStaticsCleanup()
        {
            stations.Clear();
        }

        public static void Add(Station station)
        {
            if (!stations.Contains(station))
            {
                stations.Add(station);
            }
        }

        public static void Remove(Station station)
        {
            stations.Remove(station);
        }

        /// <summary>The station whose area contains <paramref name="position"/>, the closest one first.</summary>
        public static Station FindAt(Vector3 position)
        {
            Station best = null;
            float bestDistance = float.MaxValue;
            foreach (var station in stations)
            {
                float distance = Vector3.Distance(Flat(station.owner.transform.position), Flat(position));
                if (distance < station.Radius && distance < bestDistance)
                {
                    best = station;
                    bestDistance = distance;
                }
            }
            return best;
        }

        public static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0.0f, v.z);
        }
    }

    /// <summary>
    /// One exhibit of the gallery: its name, what it demonstrates, and a live status line the HUD shows
    /// while the hero stands in its area. Exhibits with behavior derive from it.
    /// </summary>
    [ScriptSourceFile]
    public class Station : ScriptComponent
    {
        [Tooltip("Station name in the HUD.")]
        public string Title = "Station";

        [Tooltip("What the station demonstrates, as the HUD prints it.")]
        public string Description = "";

        [Tooltip("The HUD shows this station while the hero is this close to its center (m).")]
        public float Radius = 6.0f;

        [Tooltip("Point the hero glances at while near (optional).")]
        public Entity Focus;

        public override void OnStart()
        {
            StationRegistry.Add(this);
        }

        public override void OnDestroy()
        {
            StationRegistry.Remove(this);
        }

        /// <summary>Live values for the HUD, one per line.</summary>
        public virtual string GetStatus()
        {
            return "";
        }

        /// <summary>World position of the focus point, or the station center a little above the floor.</summary>
        public Vector3 GetFocusPosition()
        {
            if (Focus.IsValid())
            {
                return Focus.transform.position;
            }
            return owner.transform.position + new Vector3(0.0f, 1.4f, 0.0f);
        }
    }
}
