// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ExceptionExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay.Match.Components;
using osuTK;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Game.Screens.OnlinePlay.Multiplayer.Match
{
    public partial class MultiplayerMatchSettingsOverlay : RoomSettingsOverlay
    {
        protected override OsuButton SubmitButton => settings.ApplyButton;
        protected override bool IsLoading => ongoingOperationTracker.InProgress.Value;

        [Resolved]
        private OngoingOperationTracker ongoingOperationTracker { get; set; } = null!;

        private MatchSettings settings = null!;

        public MultiplayerMatchSettingsOverlay(Room room)
            : base(room) { }

        protected override void SelectBeatmap() => settings.SelectBeatmap();

        protected override Drawable CreateSettings(Room room) =>
            settings = new MatchSettings(room)
            {
                RelativeSizeAxes = Axes.Both,
                RelativePositionAxes = Axes.Y,
                SettingsApplied = Hide,
            };

        protected partial class MatchSettings : CompositeDrawable
        {
            private const float disabled_alpha = 0.2f;

            public override bool IsPresent => base.IsPresent || Scheduler.HasPendingTasks;

            public Action? SettingsApplied;

            public OsuTextBox NameField = null!;
            public OsuTextBox MaxParticipantsField = null!;
            public MatchTypePicker TypePicker = null!;
            public OsuEnumDropdown<QueueMode> QueueModeDropdown = null!;
            public OsuTextBox PasswordTextBox = null!;
            public OsuCheckbox AutoSkipCheckbox = null!;
            public RoundedButton ApplyButton = null!;
            public OsuSpriteText ErrorText = null!;

            private OsuEnumDropdown<StartMode> startModeDropdown = null!;
            private OsuSpriteText typeLabel = null!;
            private LoadingLayer loadingLayer = null!;

            public void SelectBeatmap() => selectBeatmapButton.TriggerClick();

            [Resolved]
            private MultiplayerMatchSubScreen matchSubScreen { get; set; } = null!;

            [Resolved]
            private MultiplayerClient client { get; set; } = null!;

            [Resolved]
            private OngoingOperationTracker ongoingOperationTracker { get; set; } = null!;

            private readonly IBindable<bool> operationInProgress = new BindableBool();
            private readonly Room room;

            private IDisposable? applyingSettingsOperation;
            private Drawable playlistContainer = null!;
            private DrawableRoomPlaylist drawablePlaylist = null!;
            private RoundedButton selectBeatmapButton = null!;

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
                                        new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 10),
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    Anchor = Anchor.TopCentre,
                                                    Origin = Anchor.TopCentre,
                                                    Padding = new MarginPadding
                                                    {
                                                        Horizontal =
                                                            WaveOverlayContainer.WIDTH_PADDING,
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
                                                                            RelativeSizeAxes =
                                                                                Axes.X,
                                                                            TabbableContentContainer =
                                                                                this,
                                                                            LengthLimit = 100,
                                                                        },
                                                                },
                                                                // new Section("Room visibility")
                                                                // {
                                                                //     Alpha = disabled_alpha,
                                                                //     Child = AvailabilityPicker = new RoomAvailabilityPicker
                                                                //     {
                                                                //         Enabled = { Value = false }
                                                                //     },
                                                                // },
                                                                new Section("Game type")
                                                                {
                                                                    Child = new FillFlowContainer
                                                                    {
                                                                        AutoSizeAxes = Axes.Y,
                                                                        RelativeSizeAxes = Axes.X,
                                                                        Direction =
                                                                            FillDirection.Vertical,
                                                                        Spacing = new Vector2(7),
                                                                        Children = new Drawable[]
                                                                        {
                                                                            TypePicker =
                                                                                new MatchTypePicker
                                                                                {
                                                                                    RelativeSizeAxes =
                                                                                        Axes.X,
                                                                                },
                                                                            typeLabel =
                                                                                new OsuSpriteText
                                                                                {
                                                                                    Font =
                                                                                        OsuFont.GetFont(
                                                                                            size: 14
                                                                                        ),
                                                                                    Colour =
                                                                                        colours.Yellow,
                                                                                },
                                                                        },
                                                                    },
                                                                },
                                                                new Section("Queue mode")
                                                                {
                                                                    Child = new Container
                                                                    {
                                                                        RelativeSizeAxes = Axes.X,
                                                                        Height = 40,
                                                                        Child = QueueModeDropdown =
                                                                            new OsuEnumDropdown<QueueMode>
                                                                            {
                                                                                RelativeSizeAxes =
                                                                                    Axes.X,
                                                                            },
                                                                    },
                                                                },
                                                                new Section("Auto start")
                                                                {
                                                                    Child = new Container
                                                                    {
                                                                        RelativeSizeAxes = Axes.X,
                                                                        Height = 40,
                                                                        Child = startModeDropdown =
                                                                            new OsuEnumDropdown<StartMode>
                                                                            {
                                                                                RelativeSizeAxes =
                                                                                    Axes.X,
                                                                            },
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
                                                                new Section("Max participants")
                                                                {
                                                                    Alpha = disabled_alpha,
                                                                    Child = MaxParticipantsField =
                                                                        new OsuNumberBox
                                                                        {
                                                                            RelativeSizeAxes =
                                                                                Axes.X,
                                                                            TabbableContentContainer =
                                                                                this,
                                                                            ReadOnly = true,
                                                                        },
                                                                },
                                                                new Section("Password (optional)")
                                                                {
                                                                    Child = PasswordTextBox =
                                                                        new OsuPasswordTextBox
                                                                        {
                                                                            RelativeSizeAxes =
                                                                                Axes.X,
                                                                            TabbableContentContainer =
                                                                                this,
                                                                            LengthLimit = 255,
                                                                        },
                                                                },
                                                                new Section("Other")
                                                                {
                                                                    Child = AutoSkipCheckbox =
                                                                        new OsuCheckbox
                                                                        {
                                                                            LabelText =
                                                                                "Automatically skip the beatmap intro",
                                                                        },
                                                                },
                                                            },
                                                        },
                                                    },
                                                },
                                                playlistContainer = new FillFlowContainer
                                                {
                                                    Anchor = Anchor.TopCentre,
                                                    Origin = Anchor.TopCentre,
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Width = 0.5f,
                                                    Depth = float.MaxValue,
                                                    Spacing = new Vector2(5),
                                                    Children = new Drawable[]
                                                    {
                                                        drawablePlaylist = new DrawableRoomPlaylist
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height =
                                                                DrawableRoomPlaylistItem.HEIGHT,
                                                        },
                                                        selectBeatmapButton = new RoundedButton
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 40,
                                                            Text = "Select beatmap",
                                                            Action = () =>
                                                                matchSubScreen.ShowSongSelect(),
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
                                                ApplyButton = new CreateOrUpdateButton(room)
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

                TypePicker.Current.BindValueChanged(
                    type => typeLabel.Text = type.NewValue.GetLocalisableDescription(),
                    true
                );

                operationInProgress.BindTo(ongoingOperationTracker.InProgress);
                operationInProgress.BindValueChanged(v =>
                {
                    if (v.NewValue)
                        loadingLayer.Show();
                    else
                        loadingLayer.Hide();
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                room.PropertyChanged += onRoomPropertyChanged;

                updateRoomName();
                updateRoomType();
                updateRoomQueueMode();
                updateRoomPassword();
                updateRoomAutoSkip();
                updateRoomMaxParticipants();
                updateRoomAutoStartDuration();
                updateRoomPlaylist();
            }

            private void onRoomPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                switch (e.PropertyName)
                {
                    case nameof(Room.Name):
                        updateRoomName();
                        break;

                    case nameof(Room.Type):
                        updateRoomName();
                        break;

                    case nameof(Room.QueueMode):
                        updateRoomQueueMode();
                        break;

                    case nameof(Room.Password):
                        updateRoomPassword();
                        break;

                    case nameof(Room.AutoSkip):
                        updateRoomAutoSkip();
                        break;

                    case nameof(Room.MaxParticipants):
                        updateRoomMaxParticipants();
                        break;

                    case nameof(Room.AutoStartDuration):
                        updateRoomAutoStartDuration();
                        break;

                    case nameof(Room.Playlist):
                        updateRoomPlaylist();
                        break;
                }
            }

            private void updateRoomName() => NameField.Text = room.Name;

            private void updateRoomType() => TypePicker.Current.Value = room.Type;

            private void updateRoomQueueMode() => QueueModeDropdown.Current.Value = room.QueueMode;

            private void updateRoomPassword() =>
                PasswordTextBox.Text = room.Password ?? string.Empty;

            private void updateRoomAutoSkip() => AutoSkipCheckbox.Current.Value = room.AutoSkip;

            private void updateRoomMaxParticipants() =>
                MaxParticipantsField.Text = room.MaxParticipants?.ToString();

            private void updateRoomAutoStartDuration() =>
                startModeDropdown.Current.Value = (StartMode)room.AutoStartDuration.TotalSeconds;

            private void updateRoomPlaylist() =>
                drawablePlaylist.Items.ReplaceRange(0, drawablePlaylist.Items.Count, room.Playlist);

            protected override void Update()
            {
                base.Update();

                ApplyButton.Enabled.Value =
                    room.Playlist.Count > 0
                    && NameField.Text.Length > 0
                    && !operationInProgress.Value;
                playlistContainer.Alpha = room.RoomID == null ? 1 : 0;
            }

            private void apply()
            {
                if (!ApplyButton.Enabled.Value)
                    return;

                ErrorText.FadeOut(50);

                Debug.Assert(applyingSettingsOperation == null);
                applyingSettingsOperation = ongoingOperationTracker.BeginOperation();

                // If the client is already in a room, update via the client.
                // Otherwise, update the room directly in preparation for it to be submitted to the API on match creation.
                if (client.Room != null)
                {
                    client
                        .ChangeSettings(
                            name: NameField.Text,
                            password: PasswordTextBox.Text,
                            matchType: TypePicker.Current.Value,
                            queueMode: QueueModeDropdown.Current.Value,
                            autoStartDuration: TimeSpan.FromSeconds(
                                (int)startModeDropdown.Current.Value
                            ),
                            autoSkip: AutoSkipCheckbox.Current.Value
                        )
                        .ContinueWith(t =>
                            Schedule(() =>
                            {
                                if (t.IsCompletedSuccessfully)
                                    onSuccess();
                                else
                                    onError(t.Exception, "Error changing settings");
                            })
                        );
                }
                else
                {
                    room.Name = NameField.Text;
                    room.Password = PasswordTextBox.Text;
                    room.Type = TypePicker.Current.Value;
                    room.QueueMode = QueueModeDropdown.Current.Value;
                    room.AutoStartDuration = TimeSpan.FromSeconds(
                        (int)startModeDropdown.Current.Value
                    );
                    room.AutoSkip = AutoSkipCheckbox.Current.Value;
                    room.Playlist = drawablePlaylist.Items.ToArray();

                    client
                        .CreateRoom(room)
                        .ContinueWith(t =>
                            Schedule(() =>
                            {
                                if (t.IsCompletedSuccessfully)
                                    onSuccess();
                                else
                                    onError(t.Exception, "Error creating room");
                            })
                        );
                }
            }

            private void onSuccess() =>
                Schedule(() =>
                {
                    Debug.Assert(applyingSettingsOperation != null);

                    SettingsApplied?.Invoke();

                    applyingSettingsOperation.Dispose();
                    applyingSettingsOperation = null;
                });

            private void onError(Exception? exception, string description)
            {
                if (exception is AggregateException aggregateException)
                    exception = aggregateException.AsSingular();

                string message =
                    exception?.GetHubExceptionMessage() ?? $"{description} ({exception?.Message})";

                Schedule(() =>
                {
                    Debug.Assert(applyingSettingsOperation != null);

                    // see https://github.com/ppy/osu-web/blob/2c97aaeb64fb4ed97c747d8383a35b30f57428c7/app/Models/Multiplayer/PlaylistItem.php#L48.
                    const string not_found_prefix = "beatmaps not found:";

                    if (message.StartsWith(not_found_prefix, StringComparison.Ordinal))
                        ErrorText.Text = "The selected beatmap is not available online.";
                    else
                        ErrorText.Text = message;

                    ErrorText.FadeIn(50);

                    applyingSettingsOperation.Dispose();
                    applyingSettingsOperation = null;
                });
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                room.PropertyChanged -= onRoomPropertyChanged;
            }
        }

        public partial class CreateOrUpdateButton : RoundedButton
        {
            private readonly Room room;

            public CreateOrUpdateButton(Room room)
            {
                this.room = room;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                BackgroundColour = colours.YellowDark;
            }

            protected override void Update()
            {
                base.Update();

                Text = room.RoomID == null ? "Create" : "Update";
            }
        }

        private enum StartMode
        {
            [Description("Off")]
            Off = 0,

            [Description("10 seconds")]
            Seconds10 = 10,

            [Description("30 seconds")]
            Seconds30 = 30,

            [Description("1 minute")]
            Seconds60 = 60,

            [Description("3 minutes")]
            Seconds180 = 180,

            [Description("5 minutes")]
            Seconds300 = 300,
        }
    }
}
