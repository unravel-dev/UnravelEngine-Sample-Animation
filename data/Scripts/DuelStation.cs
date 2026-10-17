using Unravel.Core;

namespace AnimationSample
{
    /// <summary>Status of the sparring state machines: each fighter's state, health and last blow.</summary>
    [ScriptSourceFile]
    public class DuelStation : Station
    {
        public Entity FighterA;
        public Entity FighterB;

        public override string GetStatus()
        {
            return Describe(FighterA) + "\n" + Describe(FighterB);
        }

        private static string Describe(Entity fighter)
        {
            if (!fighter.IsValid())
            {
                return "";
            }
            var duelist = fighter.GetComponent<Duelist>();
            var animation = fighter.GetComponent<AnimationComponent>();
            if (duelist == null || animation == null)
            {
                return "";
            }
            string state = animation.GetCurrentStateName(0);
            if (animation.IsInTransition(0))
            {
                state += " -> " + animation.GetNextStateName(0);
            }
            return duelist.DisplayName + ": " + state + "   " + duelist.Health + " hp   " + duelist.LastResult;
        }
    }
}
