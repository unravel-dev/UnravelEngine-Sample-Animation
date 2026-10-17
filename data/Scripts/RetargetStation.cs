using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// One clip, five skeletons. Every few seconds the next clip crossfades in on all characters at once
    /// from script (AnimationComponent.Blend, no controller). The UAL1 rig plays its own clips by copy;
    /// the others are Rest Aligned: each bone turns by what its source bone turned from its rest pose,
    /// motion scales with hip height, and bones named differently (Rigify, KayKit) are matched by
    /// humanoid role. The clips come from three source rigs, so every rig plays clips made for others.
    /// </summary>
    [ScriptSourceFile]
    public class RetargetStation : Station
    {
        public Entity CharacterA;
        public Entity CharacterB;
        public Entity CharacterC;
        public Entity CharacterD;
        public Entity CharacterE;

        [Tooltip("Seconds each clip plays.")]
        public float ClipSeconds = 6.0f;

        private const string UAL1 = "app:/data/Characters/MannequinUAL1/MannequinUAL1_Rig_";
        private const string Rigify = "app:/data/Characters/RigifyUAL/AnimationLibrary_Godot_Standard_";
        private const string KayKit = "app:/data/Characters/KayKit/Knight/Knight_";

        /// Clip key and the rig it was made for.
        private static readonly (string key, string source)[] Clips =
        {
            (UAL1 + "Dance_Loop.anim", "UAL1"),
            (Rigify + "Idle_Talking_Loop.anim", "Rigify"),
            (UAL1 + "Walk_Formal_Loop.anim", "UAL1"),
            (KayKit + "Cheer.anim", "KayKit"),
            (UAL1 + "Punch_Jab.anim", "UAL1"),
            (Rigify + "Crouch_Idle_Loop.anim", "Rigify"),
            (UAL1 + "Sword_Idle.anim", "UAL1"),
            (KayKit + "Spellcasting.anim", "KayKit"),
            (UAL1 + "Jog_Fwd_Loop.anim", "UAL1"),
        };

        private AnimationClip[] clips;
        private int index = -1;
        private float timer;

        public override void OnStart()
        {
            base.OnStart();
            clips = new AnimationClip[Clips.Length];
            for (int i = 0; i < Clips.Length; ++i)
            {
                clips[i] = Assets.GetAsset<AnimationClip>(Clips[i].key);
            }
            Next();
        }

        public override void OnUpdate()
        {
            timer += Time.deltaTime;
            if (timer >= ClipSeconds)
            {
                Next();
            }
        }

        private void Next()
        {
            timer = 0.0f;
            index = (index + 1) % clips.Length;
            foreach (var character in new[] { CharacterA, CharacterB, CharacterC, CharacterD, CharacterE })
            {
                if (!character.IsValid())
                {
                    continue;
                }
                var animation = character.GetComponent<AnimationComponent>();
                animation?.Blend(clips[index], 0.35f, true, false);
            }
        }

        public override string GetStatus()
        {
            if (index < 0)
            {
                return "";
            }
            string clip = Clips[index].key.Substring(Clips[index].key.LastIndexOf('/') + 1).Replace(".anim", "");
            return "Clip: " + clip + " (made for " + Clips[index].source + ")   next in " +
                   (ClipSeconds - timer).ToString("0.0") + " s";
        }
    }
}
