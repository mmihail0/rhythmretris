using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Tetris.Objects;

namespace osu.Game.Rulesets.Tetris.Objects.Drawables
{
    public partial class DrawableTetrisHitObject : DrawableHitObject<TetrisHitObject>
    {
        private HitResult? forcedResult;

        public static bool IsDangerState { get; set; }

        public static HitResult? PendingLockResult { get; set; }

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
                if (timeOffset >= 50)
                {
                    if (PendingLockResult.HasValue)
                    {
                        var pending = PendingLockResult.Value;
                        PendingLockResult = null;
                        ApplyResult(pending);
                    }
                    else
                    {
                        ApplyResult(IsDangerState ? HitResult.Miss : HitResult.Great);
                    }
                }
                return;
            }

            ApplyResult(IsDangerState ? HitResult.Miss : HitResult.Great);
        }

        public void TriggerUserHit() => UpdateResult(true);

        public void TriggerWithResult(HitResult result)
        {
            forcedResult = result;
            UpdateResult(true);
        }
    }
}
