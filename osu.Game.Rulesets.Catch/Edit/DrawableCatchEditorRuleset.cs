// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Catch.Edit
{
    public partial class DrawableCatchEditorRuleset : DrawableCatchRuleset
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        public readonly BindableDouble TimeRangeMultiplier = new BindableDouble(1);

        public DrawableCatchEditorRuleset(
            Ruleset ruleset,
            IBeatmap beatmap,
            IReadOnlyList<Mod>? mods = null
        )
            : base(ruleset, beatmap, mods) { }

        protected override void Update()
        {
            base.Update();

            double gamePlayTimeRange = GetTimeRange(Beatmap.Difficulty.ApproachRate);
            float playfieldStretch = Playfield.DrawHeight / CatchPlayfield.HEIGHT;
            TimeRange.Value = gamePlayTimeRange * TimeRangeMultiplier.Value * playfieldStretch;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            editorBeatmap.BeatmapReprocessed += onBeatmapReprocessed;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorBeatmap.IsNotNull())
                editorBeatmap.BeatmapReprocessed -= onBeatmapReprocessed;
        }

        private void onBeatmapReprocessed()
        {
            if (Playfield is CatchEditorPlayfield catchPlayfield)
            {
                catchPlayfield.Catcher.ApplyDifficulty(editorBeatmap.Difficulty);
                catchPlayfield.CatcherArea.CatcherTrails.UpdateCatcherTrailsScale(
                    catchPlayfield.Catcher.BodyScale
                );
            }
        }

        protected override Playfield CreatePlayfield() =>
            new CatchEditorPlayfield(Beatmap.Difficulty);

        public override PlayfieldAdjustmentContainer CreatePlayfieldAdjustmentContainer() =>
            new CatchEditorPlayfieldAdjustmentContainer();
    }
}
