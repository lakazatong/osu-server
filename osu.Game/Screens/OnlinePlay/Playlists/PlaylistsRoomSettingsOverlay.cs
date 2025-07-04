// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Humanizer;
using Humanizer.Localisation;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Screens.OnlinePlay.Match.Components;
using osuTK;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Game.Screens.OnlinePlay.Playlists
{
    public partial class PlaylistsRoomSettingsOverlay : RoomSettingsOverlay
    {
        public Action? EditPlaylist;

        private MatchSettings settings = null!;

        protected override OsuButton SubmitButton => settings.ApplyButton;

        protected override bool IsLoading => settings.IsLoading; // should probably be replaced with an OngoingOperationTracker.

        public PlaylistsRoomSettingsOverlay(Room room)
            : base(room) { }

        protected override void SelectBeatmap() => settings.SelectBeatmap();

        protected override Drawable CreateSettings(Room room) =>
            settings = new MatchSettings(room)
            {
                RelativeSizeAxes = Axes.Both,
                RelativePositionAxes = Axes.Y,
                EditPlaylist = () => EditPlaylist?.Invoke(),
            };

        protected partial class MatchSettings : CompositeDrawable
        {
            private const float disabled_alpha = 0.2f;

            public Action? EditPlaylist;

            public OsuTextBox NameField = null!,
                MaxParticipantsField = null!,
                MaxAttemptsField = null!;
            public OsuDropdown<TimeSpan> DurationField = null!;
            public RoomAvailabilityPicker AvailabilityPicker = null!;
            public RoundedButton ApplyButton = null!;

            public bool IsLoading => loadingLayer.State.Value == Visibility.Visible;

            public OsuSpriteText ErrorText = null!;

            private LoadingLayer loadingLayer = null!;
            private DrawableRoomPlaylist playlist = null!;
            private OsuSpriteText playlistLength = null!;

            private PurpleRoundedButton editPlaylistButton = null!;

            [Resolved]
            private IAPIProvider api { get; set; } = null!;

            [Resolved]
            private RulesetStore rulesets { get; set; } = null!;

            private IBindable<APIUser> localUser = null!;

            private readonly Room room;
            private OsuSpriteText durationNoticeText = null!;

            public MatchSettings(Room room)
            {
                this.room = room;
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider, OsuColour colours)
            {
                InternalChildren = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = colourProvider.Background4 },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        RowDimensions = new[]
                        {
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new OsuScrollContainer
                                {
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = OsuScreen.HORIZONTAL_OVERFLOW_PADDING,
                                        Vertical = 10,
                                    },
                                    RelativeSizeAxes = Axes.Both,
                                    Children = new[]
                                    {
                                        new Container
                                        {
                                            Padding = new MarginPadding
                                            {
                                                Horizontal = WaveOverlayContainer.WIDTH_PADDING,
                                            },
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Children = new Drawable[]
                                            {
                                                new SectionContainer
                                                {
                                                    Padding = new MarginPadding
                                                    {
                                                        Right = FIELD_PADDING / 2,
                                                    },
                                                    Children = new[]
                                                    {
                                                        new Section("Room name")
                                                        {
                                                            Child = NameField =
                                                                new OsuTextBox
                                                                {
                                                                    RelativeSizeAxes = Axes.X,
                                                                    TabbableContentContainer = this,
                                                                    LengthLimit = 100,
                                                                    Text = room.Name,
                                                                },
                                                        },
                                                        new Section("Duration")
                                                        {
                                                            Children = new Drawable[]
                                                            {
                                                                new Container
                                                                {
                                                                    RelativeSizeAxes = Axes.X,
                                                                    Height = 40,
                                                                    Child = DurationField =
                                                                        new DurationDropdown
                                                                        {
                                                                            RelativeSizeAxes =
                                                                                Axes.X,
                                                                        },
                                                                },
                                                                durationNoticeText =
                                                                    new OsuSpriteText
                                                                    {
                                                                        Alpha = 0,
                                                                        Colour = colours.Yellow,
                                                                    },
                                                            },
                                                        },
                                                        new Section(
                                                            "Allowed attempts (across all playlist items)"
                                                        )
                                                        {
                                                            Child = MaxAttemptsField =
                                                                new OsuNumberBox
                                                                {
                                                                    RelativeSizeAxes = Axes.X,
                                                                    TabbableContentContainer = this,
                                                                    PlaceholderText = "Unlimited",
                                                                },
                                                        },
                                                        new Section("Room visibility")
                                                        {
                                                            Alpha = disabled_alpha,
                                                            Child = AvailabilityPicker =
                                                                new RoomAvailabilityPicker
                                                                {
                                                                    Enabled = { Value = false },
                                                                },
                                                        },
                                                        new Section("Max participants")
                                                        {
                                                            Alpha = disabled_alpha,
                                                            Child = MaxParticipantsField =
                                                                new OsuNumberBox
                                                                {
                                                                    RelativeSizeAxes = Axes.X,
                                                                    TabbableContentContainer = this,
                                                                    ReadOnly = true,
                                                                },
                                                        },
                                                        new Section("Password (optional)")
                                                        {
                                                            Alpha = disabled_alpha,
                                                            Child = new OsuPasswordTextBox
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                TabbableContentContainer = this,
                                                                ReadOnly = true,
                                                            },
                                                        },
                                                    },
                                                },
                                                new SectionContainer
                                                {
                                                    Anchor = Anchor.TopRight,
                                                    Origin = Anchor.TopRight,
                                                    Padding = new MarginPadding
                                                    {
                                                        Left = FIELD_PADDING / 2,
                                                    },
                                                    Children = new[]
                                                    {
                                                        new Section("Playlist")
                                                        {
                                                            Child = new GridContainer
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Height = 448,
                                                                Content = new[]
                                                                {
                                                                    new Drawable[]
                                                                    {
                                                                        playlist =
                                                                            new PlaylistsRoomSettingsPlaylist
                                                                            {
                                                                                RelativeSizeAxes =
                                                                                    Axes.Both,
                                                                            },
                                                                    },
                                                                    new Drawable[]
                                                                    {
                                                                        playlistLength =
                                                                            new OsuSpriteText
                                                                            {
                                                                                Margin =
                                                                                    new MarginPadding
                                                                                    {
                                                                                        Vertical =
                                                                                            5,
                                                                                    },
                                                                                Colour =
                                                                                    colours.Yellow,
                                                                                Font =
                                                                                    OsuFont.GetFont(
                                                                                        size: 12
                                                                                    ),
                                                                            },
                                                                    },
                                                                    new Drawable[]
                                                                    {
                                                                        editPlaylistButton =
                                                                            new PurpleRoundedButton
                                                                            {
                                                                                RelativeSizeAxes =
                                                                                    Axes.X,
                                                                                Height = 40,
                                                                                Text =
                                                                                    "Edit playlist",
                                                                                Action = () =>
                                                                                    EditPlaylist?.Invoke(),
                                                                            },
                                                                    },
                                                                },
                                                                RowDimensions = new[]
                                                                {
                                                                    new Dimension(),
                                                                    new Dimension(
                                                                        GridSizeMode.AutoSize
                                                                    ),
                                                                    new Dimension(
                                                                        GridSizeMode.AutoSize
                                                                    ),
                                                                },
                                                            },
                                                        },
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                            new Drawable[]
                            {
                                new Container
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    Y = 2,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = colourProvider.Background5,
                                        },
                                        new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 20),
                                            Margin = new MarginPadding { Vertical = 20 },
                                            Padding = new MarginPadding
                                            {
                                                Horizontal = OsuScreen.HORIZONTAL_OVERFLOW_PADDING,
                                            },
                                            Children = new Drawable[]
                                            {
                                                ApplyButton = new CreateRoomButton
                                                {
                                                    Anchor = Anchor.BottomCentre,
                                                    Origin = Anchor.BottomCentre,
                                                    Size = new Vector2(230, 55),
                                                    Enabled = { Value = false },
                                                    Action = apply,
                                                },
                                                ErrorText = new OsuSpriteText
                                                {
                                                    Anchor = Anchor.BottomCentre,
                                                    Origin = Anchor.BottomCentre,
                                                    Alpha = 0,
                                                    Depth = 1,
                                                    Colour = colours.RedDark,
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                    loadingLayer = new LoadingLayer(true),
                };

                DurationField.Current.BindValueChanged(duration =>
                {
                    if (hasValidDuration)
                        durationNoticeText.Hide();
                    else
                    {
                        durationNoticeText.Show();
                        durationNoticeText.Text = OnlinePlayStrings.SupporterOnlyDurationNotice;
                    }
                });

                localUser = api.LocalUser.GetBoundCopy();
                localUser.BindValueChanged(populateDurations, true);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                room.PropertyChanged += onRoomPropertyChanged;

                updateRoomName();
                updateRoomAvailability();
                updateRoomMaxParticipants();
                updateRoomDuration();
                updateRoomMaxAttempts();
                updateRoomPlaylist();

                playlist.Items.BindCollectionChanged(
                    (_, __) => room.Playlist = playlist.Items.ToArray()
                );
            }

            private void onRoomPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(Room.Name):
                        updateRoomName();
                        break;

                    case nameof(Room.Availability):
                        updateRoomAvailability();
                        break;

                    case nameof(Room.MaxParticipants):
                        updateRoomMaxParticipants();
                        break;

                    case nameof(Room.Duration):
                        updateRoomDuration();
                        break;

                    case nameof(Room.MaxAttempts):
                        updateRoomMaxAttempts();
                        break;

                    case nameof(Room.Playlist):
                        updateRoomPlaylist();
                        break;
                }
            }

            private void updateRoomName() => NameField.Text = room.Name;

            private void updateRoomAvailability() =>
                AvailabilityPicker.Current.Value = room.Availability;

            private void updateRoomMaxParticipants() =>
                MaxParticipantsField.Text = room.MaxParticipants?.ToString();

            private void updateRoomDuration() =>
                DurationField.Current.Value = room.Duration ?? TimeSpan.FromMinutes(30);

            private void updateRoomMaxAttempts() =>
                MaxAttemptsField.Text = room.MaxAttempts?.ToString();

            private void updateRoomPlaylist() =>
                playlist.Items.ReplaceRange(0, playlist.Items.Count, room.Playlist);

            private void populateDurations(ValueChangedEvent<APIUser> user)
            {
                // roughly correct (see https://github.com/Humanizr/Humanizer/blob/18167e56c082449cc4fe805b8429e3127a7b7f93/readme.md?plain=1#L427)
                // if we want this to be more accurate we might consider sending an actual end time, not a time span. probably not required though.
                const int days_in_month = 31;

                DurationField.Items = new[]
                {
                    TimeSpan.FromMinutes(30),
                    TimeSpan.FromHours(1),
                    TimeSpan.FromHours(2),
                    TimeSpan.FromHours(4),
                    TimeSpan.FromHours(8),
                    TimeSpan.FromHours(12),
                    TimeSpan.FromHours(24),
                    TimeSpan.FromDays(3),
                    TimeSpan.FromDays(7),
                    TimeSpan.FromDays(14),
                    TimeSpan.FromDays(days_in_month),
                    TimeSpan.FromDays(days_in_month * 3),
                };
            }

            protected override void Update()
            {
                base.Update();

                ApplyButton.Enabled.Value = hasValidSettings;
            }

            public void SelectBeatmap() => editPlaylistButton.TriggerClick();

            private void onPlaylistChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
                playlistLength.Text = $"Length: {room.Playlist.GetTotalDuration(rulesets)}";

            private bool hasValidSettings =>
                room.RoomID == null
                && NameField.Text.Length > 0
                && room.Playlist.Count > 0
                && hasValidDuration;

            private bool hasValidDuration =>
                DurationField.Current.Value <= TimeSpan.FromDays(14) || localUser.Value.IsSupporter;

            private void apply()
            {
                if (!ApplyButton.Enabled.Value)
                    return;

                ErrorText.FadeOut(50);

                room.Name = NameField.Text;
                room.Availability = AvailabilityPicker.Current.Value;
                room.MaxParticipants = int.TryParse(
                    MaxParticipantsField.Text,
                    out int maxParticipants
                )
                    ? maxParticipants
                    : null;
                room.MaxAttempts = int.TryParse(MaxAttemptsField.Text, out int maxAttempts)
                    ? maxAttempts
                    : null;
                room.Duration = DurationField.Current.Value;

                loadingLayer.Show();

                var req = new CreateRoomRequest(room);
                req.Success += _ => loadingLayer.Hide();
                req.Failure += e => onError(req.Response?.Error ?? e.Message);
                api.Queue(req);
            }

            private void onError(string text)
            {
                // see https://github.com/ppy/osu-web/blob/2c97aaeb64fb4ed97c747d8383a35b30f57428c7/app/Models/Multiplayer/PlaylistItem.php#L48.
                const string not_found_prefix = "beatmaps not found:";

                if (text.StartsWith(not_found_prefix, StringComparison.Ordinal))
                {
                    ErrorText.Text =
                        "One or more beatmaps were not available online. Please remove or replace the highlighted items.";

                    int[] invalidBeatmapIDs = text.Substring(not_found_prefix.Length + 1)
                        .Split(", ")
                        .Select(int.Parse)
                        .ToArray();

                    foreach (var item in room.Playlist)
                    {
                        if (invalidBeatmapIDs.Contains(item.Beatmap.OnlineID))
                            item.MarkInvalid();
                    }
                }
                else
                {
                    ErrorText.Text = text;
                }

                ErrorText.FadeIn(50);
                loadingLayer.Hide();
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                room.PropertyChanged -= onRoomPropertyChanged;
            }
        }

        public partial class CreateRoomButton : RoundedButton
        {
            public CreateRoomButton()
            {
                Text = "Create";
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                BackgroundColour = colours.YellowDark;
            }
        }

        private partial class DurationDropdown : OsuDropdown<TimeSpan>
        {
            public DurationDropdown()
            {
                Menu.MaxHeight = 100;
            }

            protected override LocalisableString GenerateItemText(TimeSpan item) =>
                item.Humanize(maxUnit: TimeUnit.Month);
        }
    }
}
