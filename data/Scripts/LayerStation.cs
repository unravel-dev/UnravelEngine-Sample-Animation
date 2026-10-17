using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Layers. The barbarian walks on the base layer; an override layer masked to the spine and
    /// everything above it (arms, head) plays cheer / block / spellcast / interact on top, while its
    /// weight swings between 0 and 1 to show the blend; an additive layer adds hit flinches over
    /// whatever is playing. Both upper layers rest in an empty state, which fades them out.
    /// </summary>
    [ScriptSourceFile]
    public class LayerStation : Station
    {
        public Entity Character;
        public Entity Label;

        [Tooltip("Seconds per upper-body action.")]
        public float ActionSeconds = 3.2f;

        private static readonly string[] Actions = { "None", "Cheer", "Block", "Cast", "Interact" };

        private AnimationComponent animation;
        private TextComponent label;
        private float time;
        private float actionTimer;
        private float hitTimer;
        private int action;
        private float weight;

        public override void OnStart()
        {
            base.OnStart();
            animation = Character.IsValid() ? Character.GetComponent<AnimationComponent>() : null;
            label = Label.IsValid() ? Label.GetComponent<TextComponent>() : null;
        }

        public override void OnUpdate()
        {
            if (animation == null)
            {
                return;
            }
            float dt = Time.deltaTime;
            time += dt;
            actionTimer += dt;
            hitTimer += dt;
            if (actionTimer >= ActionSeconds)
            {
                actionTimer = 0.0f;
                action = (action + 1) % Actions.Length;
                animation.SetInt("UpperState", action);
            }
            weight = 0.55f + 0.45f * Mathf.Sin(time * 0.9f);
            animation.SetLayerWeight(animation.GetLayerIndex("UpperBody"), weight);
            if (hitTimer >= 2.3f)
            {
                hitTimer = 0.0f;
                animation.SetTrigger("Hit");
            }
            animation.SetFloat("Speed", 0.5f + 0.5f * Mathf.Sin(time * 0.35f));
            if (label != null)
            {
                label.text = "Upper body: " + Actions[action] + "  weight " + weight.ToString("0.00");
            }
        }

        public override string GetStatus()
        {
            if (animation == null)
            {
                return "";
            }
            return "Base: " + animation.GetCurrentStateName(0) + "\nUpperBody (override, masked): " + Actions[action] +
                   "  weight " + weight.ToString("0.00") + "\nReactions (additive): " + animation.GetCurrentStateName(2);
        }
    }
}
