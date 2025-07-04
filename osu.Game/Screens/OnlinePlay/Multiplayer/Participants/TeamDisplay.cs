// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.MatchTypes.TeamVersus;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.OnlinePlay.Multiplayer.Participants
{
    internal partial class TeamDisplay : CompositeDrawable, IHasCurrentValue<MultiplayerRoomUser>
    {
        public Bindable<MultiplayerRoomUser> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private readonly BindableWithCurrent<MultiplayerRoomUser> current =
            new BindableWithCurrent<MultiplayerRoomUser>(new MultiplayerRoomUser(-1));

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private MultiplayerClient client { get; set; } = null!;

        private OsuClickableContainer clickableContent = null!;
        private Drawable box = null!;
        private Sample? sampleTeamSwap;

        public TeamDisplay()
        {
            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;
            Margin = new MarginPadding { Horizontal = 3 };
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            InternalChild = clickableContent = new OsuClickableContainer
            {
                Width = 15,
                Alpha = 0,
                Scale = new Vector2(0, 1),
                RelativeSizeAxes = Axes.Y,
                Child = box =
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        CornerRadius = 5,
                        Masking = true,
                        Scale = new Vector2(0, 1),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Child = new Box
                        {
                            Colour = Color4.White,
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        },
                    },
            };

            sampleTeamSwap = audio.Samples.Get(@"Multiplayer/team-swap");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            client.RoomUpdated += onRoomUpdated;
            current.BindValueChanged(_ => updateUser(), true);
        }

        private void changeTeam()
        {
            client
                .SendMatchRequest(
                    new ChangeTeamRequest
                    {
                        TeamID =
                            ((client.LocalUser?.MatchState as TeamVersusUserState)?.TeamID + 1) % 2
                            ?? 0,
                    }
                )
                .FireAndForget();
        }

        public int? DisplayedTeam { get; private set; }

        private void updateUser()
        {
            var user = current.Value;

            if (client.LocalUser?.Equals(user) == true)
            {
                clickableContent.Action = changeTeam;
                clickableContent.TooltipText = "Change team";
            }

            // reset to ensure samples don't play
            DisplayedTeam = null;
            updateState();
        }

        private void onRoomUpdated() => Scheduler.AddOnce(updateState);

        private void updateState()
        {
            // we don't have a way of knowing when an individual user's state has updated, so just handle on RoomUpdated for now.

            var user = current.Value;
            var userRoomState = client.Room?.Users.FirstOrDefault(u => u.Equals(user))?.MatchState;

            const double duration = 400;

            int? newTeam = (userRoomState as TeamVersusUserState)?.TeamID;

            if (newTeam == DisplayedTeam)
                return;

            // only play the sample if an already valid team changes to another valid team.
            // this avoids playing a sound for each user if the match type is changed to/from a team mode.
            if (newTeam != null && DisplayedTeam != null)
                sampleTeamSwap?.Play();

            DisplayedTeam = newTeam;

            if (DisplayedTeam != null)
            {
                box.FadeColour(getColourForTeam(DisplayedTeam.Value), duration, Easing.OutQuint);
                box.ScaleTo(new Vector2(box.Scale.X < 0 ? 1 : -1, 1), duration, Easing.OutQuint);

                clickableContent.ScaleTo(Vector2.One, duration, Easing.OutQuint);
                clickableContent.FadeIn(duration);
            }
            else
            {
                clickableContent.ScaleTo(new Vector2(0, 1), duration, Easing.OutQuint);
                clickableContent.FadeOut(duration);
            }
        }

        private ColourInfo getColourForTeam(int id)
        {
            switch (id)
            {
                default:
                    return colours.Red;

                case 1:
                    return colours.Blue;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (client.IsNotNull())
                client.RoomUpdated -= onRoomUpdated;
        }
    }
}
