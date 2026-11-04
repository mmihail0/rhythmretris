using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Tetris.Objects;

namespace osu.Game.Rulesets.Tetris.Beatmaps
{
    public class TetrisBeatmap : Beatmap<TetrisHitObject>
    {
        public override IEnumerable<BeatmapStatistic> GetStatistics() => new List<BeatmapStatistic>();
    }
}
