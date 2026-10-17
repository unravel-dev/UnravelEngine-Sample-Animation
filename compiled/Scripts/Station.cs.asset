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

        [Tooltip("Point the hero glances at while near, when the exhibit has no characters (optional).")]
        public Entity Focus;

        /// <summary>The hero looks no closer than this (m): its head would twist to the side.</summary>
        private const float MinFocusDistance = 1.0f;
        /// <summary>Nor further off its facing than this (degrees): the look fades out behind it.</summary>
        private const float MaxFocusAngle = 75.0f;
        /// <summary>A character the hero already looks at keeps its gaze until another is this much closer (m).</summary>
        private const float FocusStickiness = 0.75f;
        /// <summary>Above the feet of a character whose head bone has another name (m, at scale 1).</summary>
        private const float FallbackHeadHeight = 1.5f;
        /// <summary>Top of a head above its bone when the character has no HeadTop anchor (m, at scale 1).</summary>
        private const float DefaultHeadTop = 0.27f;
        private static readonly string[] HeadBoneNames = { "Head", "head", "DEF-head", "mixamorig:Head" };
        /// <summary>Optional child of the head bone at the top of the head (helmets, big heads).</summary>
        private const string HeadTopName = "HeadTop";

        private readonly List<Entity> characters = new List<Entity>();
        private readonly List<Entity> heads = new List<Entity>();
        private readonly List<Entity> headTops = new List<Entity>();
        private int focusIndex = -1;

        public override void OnStart()
        {
            StationRegistry.Add(this);
            foreach (var animation in owner.GetComponentsInChildren<AnimationComponent>())
            {
                Entity head = FindHead(animation.owner);
                characters.Add(animation.owner);
                heads.Add(head);
                headTops.Add(head.IsValid() ? head.transform.FindChild(HeadTopName, false) : new Entity());
            }
        }

        private static Entity FindHead(Entity character)
        {
            foreach (var name in HeadBoneNames)
            {
                Entity head = character.transform.FindChild(name, true);
                if (head.IsValid())
                {
                    return head;
                }
            }
            return new Entity();
        }

        private Vector3 GetHeadPosition(int index)
        {
            if (heads[index].IsValid())
            {
                return heads[index].transform.position;
            }
            TransformComponent character = characters[index].transform;
            return character.position + new Vector3(0.0f, FallbackHeadHeight * character.scale.y, 0.0f);
        }

        /// <summary>
        /// How far the top of the head reaches from <see cref="GetHeadPosition"/> (m). The anchor's distance, not
        /// its height: a big head that tilts or lies down still keeps the marker clear of it.
        /// </summary>
        private float GetHeadTopHeight(int index)
        {
            if (headTops[index].IsValid())
            {
                return Vector3.Distance(headTops[index].transform.position, GetHeadPosition(index));
            }
            return DefaultHeadTop * characters[index].transform.scale.y;
        }

        private static bool IsInView(Vector3 from, Vector3 forward, Vector3 point)
        {
            Vector3 to = StationRegistry.Flat(point - from);
            Vector3 facing = StationRegistry.Flat(forward);
            return to.magnitude >= MinFocusDistance && facing.sqrMagnitude > 1e-6f &&
                   Vector2.Angle(new Vector2(facing.x, facing.z), new Vector2(to.x, to.z)) <= MaxFocusAngle;
        }

        /// <summary>
        /// What the hero at <paramref name="from"/>, facing <paramref name="forward"/>, glances at: the head of
        /// the nearest character of the exhibit, else the Focus entity. <paramref name="headTop"/> is how far
        /// the top of that head rises above the point (0 for the Focus entity). False when nothing is in
        /// front of the hero and far enough away to look at.
        /// </summary>
        public bool TryGetFocus(Vector3 from, Vector3 forward, out Vector3 point, out float headTop)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < characters.Count; ++i)
            {
                if (!characters[i].IsValid() || !characters[i].active)
                {
                    continue;
                }
                Vector3 head = GetHeadPosition(i);
                if (!IsInView(from, forward, head))
                {
                    continue;
                }
                float distance = Vector3.Distance(StationRegistry.Flat(head), StationRegistry.Flat(from)) -
                                 (i == focusIndex ? FocusStickiness : 0.0f);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            focusIndex = best;
            if (best >= 0)
            {
                point = GetHeadPosition(best);
                headTop = GetHeadTopHeight(best);
                return true;
            }
            headTop = 0.0f;
            point = Focus.IsValid() ? Focus.transform.position : Vector3.zero;
            return Focus.IsValid() && IsInView(from, forward, point);
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

    }
}
