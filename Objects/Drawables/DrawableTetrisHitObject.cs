using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Tetris.Objects;

namespace osu.Game.Rulesets.Tetris.Objects.Drawables
{
    public partial class DrawableTetrisHitObject : DrawableHitObject<TetrisHitObject>
    {
        private HitResult? forcedResult;

        public static bool IsDangerState { get; set; }

        public DrawableTetrisHitObject(TetrisHitObject hitObject)
            : base(hitObject)
        {
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (userTriggered && forcedResult.HasValue)
            {
                ApplyResult(forcedResult.Value);
                forcedResult = null;
                return;
            }

            if (!userTriggered)
            {
                if (timeOffset > 150)
                {
                    // Auto-pass notes while pieces are falling so combo builds, unless stack is in danger zone
                    ApplyResult(IsDangerState ? HitResult.Miss : HitResult.Great);
                }
                return;
            }

            double absOffset = System.Math.Abs(timeOffset);

            if (absOffset <= 75)
                ApplyResult(HitResult.Great);
            else if (absOffset <= 125)
                ApplyResult(HitResult.Ok);
            else if (absOffset <= 170)
                ApplyResult(HitResult.Meh);
            else
                ApplyResult(HitResult.Miss);
        }

        public void TriggerUserHit() => UpdateResult(true);

        public void TriggerWithResult(HitResult result)
        {
            forcedResult = result;
            UpdateResult(true);
        }
    }
}
