// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Screens;
using osuTK;

namespace osu.Game.Overlays.Settings.Sections.Maintenance
{
    public partial class MigrationRunScreen : OsuScreen
    {
        private readonly DirectoryInfo destination;

        [Resolved(canBeNull: true)]
        private OsuGame game { get; set; }

        public override bool AllowUserExit => false;

        public override bool AllowExternalScreenChange => false;

        public override bool DisallowExternalBeatmapRulesetChanges => true;

        public override bool HideOverlaysOnEnter => true;

        private Task migrationTask;

        public MigrationRunScreen(DirectoryInfo destination)
        {
            this.destination = destination;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Spacing = new Vector2(10),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = MaintenanceSettingsStrings.MigrationInProgress,
                            Font = OsuFont.Default.With(size: 40),
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = MaintenanceSettingsStrings.MigrationDescription,
                            Font = OsuFont.Default.With(size: 30),
                        },
                        new LoadingSpinner(true) { State = { Value = Visibility.Visible } },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = MaintenanceSettingsStrings.ProhibitedInteractDuringMigration,
                            Font = OsuFont.Default.With(size: 30),
                        },
                    },
                },
            };

            Beatmap.Value = Beatmap.Default;

            migrationTask = Task.Run(PerformMigration)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Logger.Error(
                            task.Exception,
                            $"Error during migration: {task.Exception?.Message}"
                        );
                    }

                    Schedule(this.Exit);
                });
        }

        protected virtual bool PerformMigration() => game?.Migrate(destination.FullName) != false;

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            this.FadeOut().Delay(250).Then().FadeIn(250);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            // block until migration is finished
            if (migrationTask?.IsCompleted == false)
                return true;

            return base.OnExiting(e);
        }
    }
}
