using System.Collections.Generic;
using System.Threading;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Tetris.Objects;

namespace osu.Game.Rulesets.Tetris.Beatmaps
{
    public class TetrisBeatmapConverter : BeatmapConverter<TetrisHitObject>
    {
        public TetrisBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
            : base(beatmap, ruleset)
        {
        }

        public override bool CanConvert() => true;

        protected override Beatmap<TetrisHitObject> CreateBeatmap() => new TetrisBeatmap();

        protected override IEnumerable<TetrisHitObject> ConvertHitObject(
            HitObject original,
            IBeatmap beatmap,
            CancellationToken cancellationToken)
        {
            yield return new TetrisHitObject
            {
                StartTime = original.StartTime
            };
        }
    }
}
