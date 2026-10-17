using Unravel.Core;

namespace AnimationSample
{
    /// <summary>Status of the events and curves exhibit: the mage's Glow curve and the rogue's last hit.</summary>
    [ScriptSourceFile]
    public class EventsStation : Station
    {
        public Entity Mage;
        public Entity Rogue;
        public Entity Dummy;

        public override string GetStatus()
        {
            string line = "";
            var caster = Mage.IsValid() ? Mage.GetComponent<SpellCaster>() : null;
            if (caster != null)
            {
                var animation = Mage.GetComponent<AnimationComponent>();
                line += "Mage: " + animation.GetCurrentStateName(0) + "   Glow curve " + caster.Glow.ToString("0.00");
            }
            var attacker = Rogue.IsValid() ? Rogue.GetComponent<DummyAttacker>() : null;
            var dummy = Dummy.IsValid() ? Dummy.GetComponent<TrainingDummy>() : null;
            if (attacker != null && dummy != null)
            {
                line += "\nRogue: last hit '" + attacker.LastAttack + "'   dummy struck " + dummy.Hits + " times";
            }
            return line;
        }
    }
}
