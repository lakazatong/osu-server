﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Timing;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;
using osuTK.Graphics;

namespace osu.Game.Tests.Visual.Gameplay
{
    [TestFixture]
    public partial class TestSceneBreakTracker : OsuTestScene
    {
        private readonly BreakOverlay breakOverlay;

        private readonly TestBreakTracker breakTracker;

        private readonly IReadOnlyList<BreakPeriod> testBreaks = new List<BreakPeriod>
        {
            new BreakPeriod(1000, 5000),
            new BreakPeriod(6000, 13500),
        };

        public TestSceneBreakTracker()
        {
            Children = new Drawable[]
            {
                new Box { Colour = Color4.White, RelativeSizeAxes = Axes.Both },
                breakTracker = new TestBreakTracker(),
                breakOverlay = new BreakOverlay(new ScoreProcessor(new OsuRuleset()))
                {
                    ProcessCustomClock = false,
                    BreakTracker = breakTracker,
                },
                new LetterboxOverlay { ProcessCustomClock = false, BreakTracker = breakTracker },
            };
        }

        protected override void Update()
        {
            base.Update();

            breakOverlay.Clock = breakTracker.Clock;
        }

        [Test]
        public void TestShowBreaks()
        {
            addShowBreakStep(5);
            addShowBreakStep(15);
        }

        [Test]
        public void TestNoEffectsBreak()
        {
            var shortBreak = new BreakPeriod(0, 500);

            setClock(true);
            loadBreaksStep("short break", new[] { shortBreak });

            addBreakSeeks(shortBreak, false);
        }

        [Test]
        public void TestMultipleBreaks()
        {
            setClock(true);
            loadBreaksStep("multiple breaks", testBreaks);

            foreach (var b in testBreaks)
                addBreakSeeks(b, false);
        }

        [Test]
        public void TestRewindBreaks()
        {
            setClock(true);
            loadBreaksStep("multiple breaks", testBreaks);

            foreach (var b in testBreaks.Reverse())
                addBreakSeeks(b, true);
        }

        [Test]
        public void TestSkipBreaks()
        {
            setClock(true);
            loadBreaksStep("multiple breaks", testBreaks);

            seekAndAssertBreak("seek to break start", testBreaks[1].StartTime, true);
            seekAndAssertBreak(
                "seek to break middle",
                testBreaks[1].StartTime + testBreaks[1].Duration / 2,
                true
            );
            seekAndAssertBreak("seek to break end", testBreaks[1].EndTime, false);
            seekAndAssertBreak("seek to break after end", testBreaks[1].EndTime + 500, false);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestBeforeGameplayStart(bool withBreaks)
        {
            setClock(true);

            if (withBreaks)
                loadBreaksStep("multiple breaks", testBreaks);

            seekAndAssertBreak("seek to break intro time", -100, true);
            seekAndAssertBreak("seek to break intro time", 0, false);
        }

        private void addShowBreakStep(double seconds)
        {
            AddStep(
                $"show '{seconds}s' break",
                () =>
                {
                    breakTracker.Breaks = new List<BreakPeriod>
                    {
                        new BreakPeriod(Clock.CurrentTime, Clock.CurrentTime + seconds * 1000),
                    };
                }
            );
        }

        private void setClock(bool useManual)
        {
            AddStep(
                $"set {(useManual ? "manual" : "realtime")} clock",
                () => breakTracker.SwitchClock(useManual)
            );
        }

        private void loadBreaksStep(string breakDescription, IReadOnlyList<BreakPeriod> breaks)
        {
            AddStep($"load {breakDescription}", () => breakTracker.Breaks = breaks);
            seekAndAssertBreak("seek back to 0", 0, false);
        }

        private void addBreakSeeks(BreakPeriod b, bool isReversed)
        {
            if (isReversed)
            {
                seekAndAssertBreak("seek to break after end", b.EndTime + 500, false);
                seekAndAssertBreak("seek to break end", b.EndTime, false);
                seekAndAssertBreak(
                    "seek to break middle",
                    b.StartTime + b.Duration / 2,
                    b.HasEffect
                );
                seekAndAssertBreak("seek to break start", b.StartTime, b.HasEffect);
            }
            else
            {
                seekAndAssertBreak("seek to break start", b.StartTime, b.HasEffect);
                seekAndAssertBreak(
                    "seek to break middle",
                    b.StartTime + b.Duration / 2,
                    b.HasEffect
                );
                seekAndAssertBreak("seek to break end", b.EndTime, false);
                seekAndAssertBreak("seek to break after end", b.EndTime + 500, false);
            }
        }

        private void seekAndAssertBreak(string seekStepDescription, double time, bool shouldBeBreak)
        {
            AddStep(seekStepDescription, () => breakTracker.ManualClockTime = time);
            AddAssert(
                $"is{(!shouldBeBreak ? " not" : string.Empty)} break time",
                () =>
                {
                    breakTracker.ProgressTime();
                    return breakTracker.IsBreakTime.Value == shouldBeBreak;
                }
            );
        }

        private partial class TestBreakTracker : BreakTracker
        {
            public readonly FramedClock FramedManualClock;

            private readonly ManualClock manualClock;
            private IFrameBasedClock originalClock;

            public double ManualClockTime
            {
                get => manualClock.CurrentTime;
                set => manualClock.CurrentTime = value;
            }

            public TestBreakTracker()
                : base(0, new ScoreProcessor(new OsuRuleset()))
            {
                FramedManualClock = new FramedClock(manualClock = new ManualClock());
                ProcessCustomClock = false;
            }

            public void ProgressTime()
            {
                FramedManualClock.ProcessFrame();
                Update();
            }

            public void SwitchClock(bool setManual) =>
                Clock = setManual ? FramedManualClock : originalClock;

            protected override void LoadComplete()
            {
                base.LoadComplete();
                originalClock = Clock;
            }
        }
    }
}
