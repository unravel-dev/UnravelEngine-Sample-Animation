using Unravel.Core;

namespace AnimationSample
{
    /// <summary>
    /// Spring bones: secondary motion simulated on top of the animated pose. Bead strands hang from a
    /// bar and part around a swinging ball (a sphere collider every strand knows), lanterns sway under a
    /// turning beam, and the hero's charm swings as it runs. Stiffness pulls each joint back to its
    /// animated direction, damping bleeds speed, gravity drags it down.
    /// </summary>
    [ScriptSourceFile]
    public class SpringStation : Station
    {
        [Tooltip("Entity whose children are the strand roots (each with a Spring Bone).")]
        public Entity Strands;

        [Tooltip("The swinging ball the strands avoid.")]
        public Entity Ball;

        [Tooltip("Ball radius (m).")]
        public float BallRadius = 0.32f;

        private int strandCount;

        public override void OnStart()
        {
            base.OnStart();
            if (!Strands.IsValid() || !Ball.IsValid())
            {
                return;
            }
            // Colliders are entity references, so they are wired here rather than per strand in the scene.
            foreach (var strand in Strands.transform.children)
            {
                var spring = strand.GetComponent<SpringBoneComponent>();
                if (spring == null)
                {
                    continue;
                }
                spring.ClearColliders();
                spring.AddCollider(Ball, BallRadius, Vector3.zero);
                strandCount++;
            }
        }

        public override string GetStatus()
        {
            return strandCount + " bead strands colliding with the ball\nGravity, stiffness and damping per Spring Bone component";
        }
    }
}
