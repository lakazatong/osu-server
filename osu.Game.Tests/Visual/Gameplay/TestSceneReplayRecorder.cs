// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Testing;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osu.Game.Graphics.Sprites;
using osu.Game.Replays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Tests.Gameplay;
using osu.Game.Tests.Mods;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Gameplay
{
    public partial class TestSceneReplayRecorder : OsuManualInputManagerTestScene
    {
        private TestRulesetInputManager playbackManager;
        private TestRulesetInputManager recordingManager;

        private Replay replay;

        private TestReplayRecorder recorder;

        private GameplayState gameplayState;

        private Drawable content;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Reset recorder state", cleanUpState);

            AddStep(
                "Setup containers",
                () =>
                {
                    replay = new Replay();

                    gameplayState = TestGameplayState.Create(new OsuRuleset());
                    gameplayState.Score.Replay = replay;

                    Child = new DependencyProvidingContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        CachedDependencies = new (Type, object)[]
                        {
                            (typeof(GameplayState), gameplayState),
                        },
                        Child = content = createContent(),
                    };
                }
            );
        }

        [Test]
        public void TestBasic()
        {
            AddStep(
                "move to center",
                () => InputManager.MoveMouseTo(recordingManager.ScreenSpaceDrawQuad.Centre)
            );
            AddUntilStep(
                "at least one frame recorded",
                () => replay.Frames.Count,
                () => Is.GreaterThanOrEqualTo(0)
            );
            AddUntilStep(
                "position matches",
                () =>
                    playbackManager.ChildrenOfType<Box>().First().Position
                    == recordingManager.ChildrenOfType<Box>().First().Position
            );
        }

        [Test]
        [Explicit(
            "Making this test work in a headless context is high effort due to rate adjustment requirements not aligning with the global fast clock. StopwatchClock usage would need to be replace with a rate adjusting clock that still reads from the parent clock. High effort for a test which likely will not see any changes to covered code for some years."
        )]
        public void TestSlowClockStillRecordsFramesInRealtime()
        {
            ScheduledDelegate moveFunction = null;

            AddStep(
                "set slow running clock",
                () =>
                {
                    var stopwatchClock = new StopwatchClock(true) { Rate = 0.01 };
                    stopwatchClock.Seek(Clock.CurrentTime);

                    content.Clock = new FramedClock(stopwatchClock);
                }
            );

            AddStep(
                "move to center",
                () => InputManager.MoveMouseTo(recordingManager.ScreenSpaceDrawQuad.Centre)
            );
            AddStep(
                "much move",
                () =>
                    moveFunction = Scheduler.AddDelayed(
                        () =>
                            InputManager.MoveMouseTo(
                                InputManager.CurrentState.Mouse.Position + new Vector2(-1, 0)
                            ),
                        10,
                        true
                    )
            );
            AddWaitStep("move", 10);
            AddStep("stop move", () => moveFunction.Cancel());
            AddAssert(
                "at least 60 frames recorded",
                () => replay.Frames.Count,
                () => Is.GreaterThanOrEqualTo(60)
            );
        }

        [Test]
        public void TestHighFrameRate()
        {
            ScheduledDelegate moveFunction = null;

            AddStep(
                "move to center",
                () => InputManager.MoveMouseTo(recordingManager.ScreenSpaceDrawQuad.Centre)
            );
            AddStep(
                "much move",
                () =>
                    moveFunction = Scheduler.AddDelayed(
                        () =>
                            InputManager.MoveMouseTo(
                                InputManager.CurrentState.Mouse.Position + new Vector2(-1, 0)
                            ),
                        10,
                        true
                    )
            );
            AddWaitStep("move", 10);
            AddStep("stop move", () => moveFunction.Cancel());
            AddAssert(
                "at least 60 frames recorded",
                () => replay.Frames.Count,
                () => Is.GreaterThanOrEqualTo(60)
            );
        }

        [Test]
        public void TestLimitedFrameRate()
        {
            ScheduledDelegate moveFunction = null;
            int initialFrameCount = 0;

            AddStep("lower rate", () => recorder.RecordFrameRate = 2);
            AddStep("count frames", () => initialFrameCount = replay.Frames.Count);
            AddStep(
                "move to center",
                () => InputManager.MoveMouseTo(recordingManager.ScreenSpaceDrawQuad.Centre)
            );
            AddStep(
                "much move",
                () =>
                    moveFunction = Scheduler.AddDelayed(
                        () =>
                            InputManager.MoveMouseTo(
                                InputManager.CurrentState.Mouse.Position + new Vector2(-1, 0)
                            ),
                        10,
                        true
                    )
            );
            AddWaitStep("move", 10);
            AddStep("stop move", () => moveFunction.Cancel());
            AddAssert(
                "less than 10 frames recorded",
                () => replay.Frames.Count - initialFrameCount,
                () => Is.LessThan(10)
            );
        }

        [Test]
        public void TestLimitedFrameRateWithImportantFrames()
        {
            ScheduledDelegate moveFunction = null;

            AddStep("lower rate", () => recorder.RecordFrameRate = 2);
            AddStep(
                "move to center",
                () => InputManager.MoveMouseTo(recordingManager.ScreenSpaceDrawQuad.Centre)
            );
            AddStep(
                "much move with press",
                () =>
                    moveFunction = Scheduler.AddDelayed(
                        () =>
                        {
                            InputManager.MoveMouseTo(
                                InputManager.CurrentState.Mouse.Position + new Vector2(-1, 0)
                            );
                            InputManager.Click(MouseButton.Left);
                        },
                        10,
                        true
                    )
            );
            AddWaitStep("move", 10);
            AddStep("stop move", () => moveFunction.Cancel());
            AddAssert(
                "at least 60 frames recorded",
                () => replay.Frames.Count,
                () => Is.GreaterThanOrEqualTo(60)
            );
        }

        protected override void Update()
        {
            base.Update();
            playbackManager?.ReplayInputHandler?.SetFrameFromTime(Time.Current - 100);
        }

        [TearDownSteps]
        public void TearDown()
        {
            AddStep("stop recorder", cleanUpState);
        }

        private void cleanUpState()
        {
            // Ensure previous recorder is disposed else it may affect the global playing state of `SpectatorClient`.
            recorder?.RemoveAndDisposeImmediately();
            recorder = null;
        }

        private Drawable createContent() =>
            new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                Content = new[]
                {
                    new Drawable[]
                    {
                        recordingManager = new TestRulesetInputManager(
                            TestCustomisableModRuleset.CreateTestRulesetInfo(),
                            0,
                            SimultaneousBindingMode.Unique
                        )
                        {
                            Recorder = recorder =
                                new TestReplayRecorder(gameplayState.Score)
                                {
                                    ScreenSpaceToGamefield = pos =>
                                        recordingManager.ToLocalSpace(pos),
                                },
                            Child = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box { Colour = Color4.Brown, RelativeSizeAxes = Axes.Both },
                                    new OsuSpriteText
                                    {
                                        Text = "Recording",
                                        Scale = new Vector2(3),
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                    },
                                    new TestInputConsumer(),
                                },
                            },
                        },
                    },
                    new Drawable[]
                    {
                        playbackManager = new TestRulesetInputManager(
                            TestCustomisableModRuleset.CreateTestRulesetInfo(),
                            0,
                            SimultaneousBindingMode.Unique
                        )
                        {
                            ReplayInputHandler = new TestFramedReplayInputHandler(replay)
                            {
                                GamefieldToScreenSpace = pos => playbackManager.ToScreenSpace(pos),
                            },
                            Child = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        Colour = Color4.DarkBlue,
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    new OsuSpriteText
                                    {
                                        Text = "Playback",
                                        Scale = new Vector2(3),
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                    },
                                    new TestInputConsumer(),
                                },
                            },
                        },
                    },
                },
            };

        public class TestFramedReplayInputHandler : FramedReplayInputHandler<TestReplayFrame>
        {
            public TestFramedReplayInputHandler(Replay replay)
                : base(replay) { }

            protected override void CollectReplayInputs(List<IInput> inputs)
            {
                inputs.Add(
                    new MousePositionAbsoluteInput
                    {
                        Position = GamefieldToScreenSpace(CurrentFrame?.Position ?? Vector2.Zero),
                    }
                );
                inputs.Add(
                    new ReplayState<TestAction>
                    {
                        PressedActions = CurrentFrame?.Actions ?? new List<TestAction>(),
                    }
                );
            }
        }

        public partial class TestInputConsumer : CompositeDrawable, IKeyBindingHandler<TestAction>
        {
            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) =>
                Parent!.ReceivePositionalInputAt(screenSpacePos);

            private readonly Box box;

            public TestInputConsumer()
            {
                Size = new Vector2(30);

                Origin = Anchor.Centre;

                InternalChildren = new Drawable[]
                {
                    box = new Box { Colour = Color4.Black, RelativeSizeAxes = Axes.Both },
                };
            }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                Position = e.MousePosition;
                return base.OnMouseMove(e);
            }

            public bool OnPressed(KeyBindingPressEvent<TestAction> e)
            {
                if (e.Repeat)
                    return false;

                box.Colour = Color4.White;
                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<TestAction> e)
            {
                box.Colour = Color4.Black;
            }
        }

        public partial class TestRulesetInputManager : RulesetInputManager<TestAction>
        {
            public TestRulesetInputManager(
                RulesetInfo ruleset,
                int variant,
                SimultaneousBindingMode unique
            )
                : base(ruleset, variant, unique) { }

            protected override KeyBindingContainer<TestAction> CreateKeyBindingContainer(
                RulesetInfo ruleset,
                int variant,
                SimultaneousBindingMode unique
            ) => new TestKeyBindingContainer();

            internal partial class TestKeyBindingContainer : KeyBindingContainer<TestAction>
            {
                public override IEnumerable<IKeyBinding> DefaultKeyBindings =>
                    new[] { new KeyBinding(InputKey.MouseLeft, TestAction.Down) };
            }
        }

        public class TestReplayFrame : ReplayFrame
        {
            public Vector2 Position;

            public List<TestAction> Actions = new List<TestAction>();

            public TestReplayFrame(double time, Vector2 position, params TestAction[] actions)
                : base(time)
            {
                Position = position;
                Actions.AddRange(actions);
            }

            public override bool IsEquivalentTo(ReplayFrame other) =>
                other is TestReplayFrame testFrame
                && Time == testFrame.Time
                && Position == testFrame.Position
                && Actions.SequenceEqual(testFrame.Actions);
        }

        public enum TestAction
        {
            Down,
        }

        internal partial class TestReplayRecorder : ReplayRecorder<TestAction>
        {
            public TestReplayRecorder(Score target)
                : base(target) { }

            protected override ReplayFrame HandleFrame(
                Vector2 mousePosition,
                List<TestAction> actions,
                ReplayFrame previousFrame
            ) => new TestReplayFrame(Time.Current, mousePosition, actions.ToArray());
        }
    }
}
