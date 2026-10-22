using System.Collections.Generic;
using Unravel.Core;

namespace AnimationSample
{
    /// <summary>The data model "gallery" of UI/Hud.rhtml.</summary>
    public sealed class GalleryModel
    {
        public bool helpShown = true;
        public bool stationShown;
        public string stationTitle = "";
        public string stationDescription = "";
        public string stationStatus = "";
        public List<EventLogEntry> events = new List<EventLogEntry>();
        public bool hasEvents;
        public string heroState = "";
        public string heroSpeed = "";
        /// <summary>On a touch screen the cards keep clear of the on-screen controls in the bottom corners.</summary>
        public bool touch;
    }

    /// <summary>
    /// The gallery HUD: the card of the station the hero stands at (what it shows, live values), the
    /// feed of animation events around the hero, the hero's state per layer, and the controls (F1).
    /// </summary>
    [ScriptSourceFile]
    public class GalleryHud : ScriptComponent
    {
        public Entity Hero;

        [Tooltip("Seconds between refreshes of the station status.")]
        public float StatusInterval = 0.15f;

        private UIDocumentComponent document;
        private UIDataModel model;
        private GalleryModel data = new GalleryModel();
        private HeroController hero;
        private AnimationComponent heroAnimation;
        private Station shown;
        private int eventVersion = -1;
        private float statusTimer;

        public override void OnStart()
        {
            document = owner.GetComponent<UIDocumentComponent>();
            model = document.GetModel("gallery");
            // The controls card lists keys; a touch screen has none (and no F1 to hide the card).
            data.touch = Input.touchSupported;
            data.helpShown = !data.touch;
            model.Bind(data);
            if (Hero.IsValid())
            {
                hero = Hero.GetComponent<HeroController>();
                heroAnimation = Hero.GetComponent<AnimationComponent>();
            }
        }

        public override void OnUpdate()
        {
            if (Input.IsPressed(KeyCode.F1))
            {
                data.helpShown = !data.helpShown;
                model.Dirty("helpShown");
            }
            UpdateStation();
            UpdateEvents();
            UpdateHero();
        }

        private void UpdateStation()
        {
            Station station = Hero.IsValid() ? StationRegistry.FindAt(Hero.transform.position) : null;
            if (station != shown)
            {
                shown = station;
                data.stationShown = station != null;
                if (station != null)
                {
                    data.stationTitle = station.Title;
                    data.stationDescription = station.Description;
                    data.stationStatus = station.GetStatus();
                }
                model.Dirty("stationShown");
                model.Dirty("stationTitle");
                model.Dirty("stationDescription");
                model.Dirty("stationStatus");
            }
            statusTimer += Time.deltaTime;
            if (shown != null && statusTimer >= StatusInterval)
            {
                statusTimer = 0.0f;
                string status = shown.GetStatus();
                if (status != data.stationStatus)
                {
                    data.stationStatus = status;
                    model.Dirty("stationStatus");
                }
            }
        }

        private void UpdateEvents()
        {
            if (EventLog.Version == eventVersion)
            {
                return;
            }
            eventVersion = EventLog.Version;
            data.events.Clear();
            data.events.AddRange(EventLog.Entries);
            data.hasEvents = data.events.Count > 0;
            model.Dirty("events");
            model.Dirty("hasEvents");
        }

        private void UpdateHero()
        {
            if (hero == null || heroAnimation == null)
            {
                return;
            }
            string state = hero.StateLine;
            if (state != data.heroState)
            {
                data.heroState = state;
                model.Dirty("heroState");
            }
            string speed = heroAnimation.GetFloat("Speed").ToString("0.0") + " m/s";
            if (speed != data.heroSpeed)
            {
                data.heroSpeed = speed;
                model.Dirty("heroSpeed");
            }
        }
    }
}
