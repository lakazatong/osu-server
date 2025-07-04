// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Collections;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.Chat;
using osu.Game.Online.Rooms;
using osu.Game.Overlays;
using osu.Game.Overlays.BeatmapSet;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play.HUD;
using osu.Game.Users.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.OnlinePlay
{
    public partial class DrawableRoomPlaylistItem
        : OsuRearrangeableListItem<PlaylistItem>,
            IHasContextMenu
    {
        public const float HEIGHT = 50;

        private const float icon_height = 34;

        private const float border_thickness = 3;

        /// <summary>
        /// Invoked when this item requests to be deleted.
        /// </summary>
        public Action<PlaylistItem>? RequestDeletion;

        /// <summary>
        /// Invoked when this item requests its results to be shown.
        /// </summary>
        public Action<PlaylistItem>? RequestResults;

        /// <summary>
        /// Invoked when this item requests to be edited.
        /// </summary>
        public Action<PlaylistItem>? RequestEdit;

        /// <summary>
        /// The currently-selected item, used to show a border around this item.
        /// May be updated by this item if <see cref="AllowSelection"/> is <c>true</c>.
        /// </summary>
        public readonly Bindable<PlaylistItem?> SelectedItem = new Bindable<PlaylistItem?>();

        public readonly PlaylistItem Item;

        public bool IsSelectedItem => SelectedItem.Value?.ID == Item.ID;

        private readonly DelayedLoadWrapper onScreenLoader;
        private readonly IBindable<bool> valid = new Bindable<bool>();
        private readonly IBindable<bool> completed = new Bindable<bool>();

        private IBeatmapInfo? beatmap;
        private IRulesetInfo? ruleset;
        private Mod[] requiredMods = Array.Empty<Mod>();

        private Container? borderContainer;
        private FillFlowContainer? difficultyIconContainer;
        private LinkFlowContainer? beatmapText;
        private LinkFlowContainer? authorText;
        private ExplicitContentBeatmapBadge? explicitContent;
        private ModDisplay? modDisplay;
        private FillFlowContainer? buttonsFlow;
        private UpdateableAvatar? ownerAvatar;
        private Drawable? showResultsButton;
        private Drawable? editButton;
        private Drawable? removeButton;
        private PanelBackground? panelBackground;
        private FillFlowContainer? mainFillFlow;
        private BeatmapCardThumbnail? thumbnail;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private UserLookupCache userLookupCache { get; set; } = null!;

        [Resolved]
        private BeatmapLookupCache beatmapLookupCache { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private BeatmapSetOverlay? beatmapOverlay { get; set; }

        [Resolved(CanBeNull = true)]
        private ManageCollectionsDialog? manageCollectionsDialog { get; set; }

        public DrawableRoomPlaylistItem(PlaylistItem item, bool loadImmediately = false)
            : base(item)
        {
            onScreenLoader = new DelayedLoadWrapper(
                Empty,
                timeBeforeLoad: loadImmediately ? 0 : 500
            )
            {
                RelativeSizeAxes = Axes.Both,
            };

            Item = item;

            valid.BindTo(item.Valid);
            completed.BindTo(item.Completed);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (borderContainer != null)
                borderContainer.BorderColour = colours.Yellow;

            ruleset = rulesets.GetRuleset(Item.RulesetID);
            var rulesetInstance = ruleset?.CreateInstance();

            if (rulesetInstance != null)
                requiredMods = Item.RequiredMods.Select(m => m.ToMod(rulesetInstance)).ToArray();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            SelectedItem.BindValueChanged(
                selected =>
                {
                    if (!valid.Value)
                    {
                        // Don't allow selection when not valid.
                        if (IsSelectedItem)
                        {
                            SelectedItem.Value = selected.OldValue;
                        }

                        // Don't update border when not valid (the border is displaying this fact).
                        return;
                    }

                    if (borderContainer != null)
                        borderContainer.BorderThickness = IsSelectedItem ? border_thickness : 0;
                },
                true
            );

            valid.BindValueChanged(_ => Scheduler.AddOnce(refresh));

            onScreenLoader.DelayedLoadStarted += _ =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (showItemOwner)
                        {
                            var foundUser = await userLookupCache
                                .GetUserAsync(Item.OwnerID)
                                .ConfigureAwait(false);
                            Schedule(() =>
                            {
                                if (ownerAvatar != null)
                                    ownerAvatar.User = foundUser;
                            });
                        }

                        beatmap = await beatmapLookupCache
                            .GetBeatmapAsync(Item.Beatmap.OnlineID)
                            .ConfigureAwait(false);

                        Scheduler.AddOnce(refresh);
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Error while populating playlist item {e}");
                    }
                });
            };

            refresh();
        }

        /// <summary>
        /// Whether this item can be selected.
        /// </summary>
        public bool AllowSelection { get; set; }

        /// <summary>
        /// Whether this item can be reordered in the playlist.
        /// </summary>
        public bool AllowReordering
        {
            get => ShowDragHandle.Value;
            set => ShowDragHandle.Value = value;
        }

        private bool allowDeletion;

        /// <summary>
        /// Whether this item can be deleted.
        /// </summary>
        public bool AllowDeletion
        {
            get => allowDeletion;
            set
            {
                allowDeletion = value;

                if (removeButton != null)
                    removeButton.Alpha = value ? 1 : 0;
            }
        }

        private bool allowShowingResults;

        /// <summary>
        /// Whether this item can have results shown.
        /// </summary>
        public bool AllowShowingResults
        {
            get => allowShowingResults;
            set
            {
                allowShowingResults = value;

                if (showResultsButton != null)
                    showResultsButton.Alpha = value ? 1 : 0;
            }
        }

        private bool allowEditing;

        /// <summary>
        /// Whether this item can be edited.
        /// </summary>
        public bool AllowEditing
        {
            get => allowEditing;
            set
            {
                allowEditing = value;

                if (editButton != null)
                    editButton.Alpha = value ? 1 : 0;
            }
        }

        private bool showItemOwner;

        /// <summary>
        /// Whether to display the avatar of the user which owns this playlist item.
        /// </summary>
        public bool ShowItemOwner
        {
            get => showItemOwner;
            set
            {
                showItemOwner = value;

                if (ownerAvatar != null)
                    ownerAvatar.Alpha = value ? 1 : 0;
            }
        }

        private void refresh()
        {
            if (borderContainer != null)
            {
                if (!valid.Value)
                {
                    borderContainer.BorderThickness = border_thickness;
                    borderContainer.BorderColour = colours.Red;
                }
            }

            if (difficultyIconContainer != null)
            {
                if (beatmap != null)
                {
                    difficultyIconContainer.Children = new Drawable[]
                    {
                        thumbnail = new BeatmapCardThumbnail(
                            beatmap.BeatmapSet!,
                            (IBeatmapSetOnlineInfo)beatmap.BeatmapSet!
                        )
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Width = 60,
                            Masking = true,
                            CornerRadius = 10,
                            RelativeSizeAxes = Axes.Y,
                            Dimmed = { Value = IsHovered },
                        },
                        new DifficultyIcon(beatmap, ruleset, requiredMods)
                        {
                            Size = new Vector2(24),
                            TooltipType = DifficultyIconTooltipType.Extended,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    };
                }
                else
                    difficultyIconContainer.Clear();
            }

            if (panelBackground != null)
                panelBackground.Beatmap.Value = beatmap;

            if (beatmapText != null)
            {
                beatmapText.Clear();

                if (beatmap != null)
                {
                    beatmapText.AddLink(
                        beatmap.GetDisplayTitleRomanisable(includeCreator: false),
                        LinkAction.OpenBeatmap,
                        beatmap.OnlineID.ToString(),
                        null,
                        text =>
                        {
                            text.Truncate = true;
                        }
                    );
                }
            }

            if (authorText != null)
            {
                authorText.Clear();

                if (!string.IsNullOrEmpty(beatmap?.Metadata.Author.Username))
                {
                    authorText.AddText("mapped by ");
                    authorText.AddUserLink(beatmap.Metadata.Author);
                }
            }

            if (explicitContent != null)
            {
                bool hasExplicitContent =
                    (beatmap?.BeatmapSet as IBeatmapSetOnlineInfo)?.HasExplicitContent == true;
                explicitContent.Alpha = hasExplicitContent ? 1 : 0;
            }

            if (modDisplay != null)
                modDisplay.Current.Value = requiredMods.ToArray();

            if (buttonsFlow != null)
            {
                buttonsFlow.Clear();
                buttonsFlow.ChildrenEnumerable = createButtons();
            }

            difficultyIconContainer.FadeInFromZero(500, Easing.OutQuint);
            mainFillFlow.FadeInFromZero(500, Easing.OutQuint);
        }

        protected override Drawable CreateContent()
        {
            Action<SpriteText> fontParameters = s =>
                s.Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold);

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = HEIGHT,
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 10,
                        Children = new Drawable[]
                        {
                            onScreenLoader,
                            panelBackground = new PanelBackground { RelativeSizeAxes = Axes.Both },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                ColumnDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(),
                                    new Dimension(GridSizeMode.AutoSize),
                                    new Dimension(GridSizeMode.AutoSize),
                                },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        difficultyIconContainer = new FillFlowContainer
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            AutoSizeAxes = Axes.X,
                                            RelativeSizeAxes = Axes.Y,
                                            Direction = FillDirection.Horizontal,
                                            Spacing = new Vector2(4),
                                            Margin = new MarginPadding { Right = 4 },
                                        },
                                        mainFillFlow = new MainFlow(() =>
                                            SelectedItem.Value == Model || !AllowSelection
                                        )
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            AutoSizeAxes = Axes.Y,
                                            RelativeSizeAxes = Axes.X,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, -2),
                                            Children = new Drawable[]
                                            {
                                                beatmapText = new LinkFlowContainer(fontParameters)
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    // workaround to ensure only the first line of text shows, emulating truncation (but without ellipsis at the end).
                                                    // TODO: remove when text/link flow can support truncation with ellipsis natively.
                                                    Height = OsuFont.DEFAULT_FONT_SIZE,
                                                    Masking = true,
                                                },
                                                new FillFlowContainer
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Horizontal,
                                                    Spacing = new Vector2(10f, 0),
                                                    Children = new Drawable[]
                                                    {
                                                        new FillFlowContainer
                                                        {
                                                            AutoSizeAxes = Axes.Both,
                                                            Anchor = Anchor.CentreLeft,
                                                            Origin = Anchor.CentreLeft,
                                                            Direction = FillDirection.Horizontal,
                                                            Spacing = new Vector2(10f, 0),
                                                            Children = new Drawable[]
                                                            {
                                                                authorText = new LinkFlowContainer(
                                                                    fontParameters
                                                                )
                                                                {
                                                                    AutoSizeAxes = Axes.Both,
                                                                },
                                                                explicitContent =
                                                                    new ExplicitContentBeatmapBadge
                                                                    {
                                                                        Alpha = 0f,
                                                                        Anchor = Anchor.CentreLeft,
                                                                        Origin = Anchor.CentreLeft,
                                                                        Margin = new MarginPadding
                                                                        {
                                                                            Top = 3f,
                                                                        },
                                                                    },
                                                            },
                                                        },
                                                        new Container
                                                        {
                                                            Anchor = Anchor.CentreLeft,
                                                            Origin = Anchor.CentreLeft,
                                                            AutoSizeAxes = Axes.Both,
                                                            Child = modDisplay =
                                                                new ModDisplay
                                                                {
                                                                    Scale = new Vector2(0.4f),
                                                                    ExpansionMode =
                                                                        ExpansionMode.AlwaysExpanded,
                                                                    Margin = new MarginPadding
                                                                    {
                                                                        Vertical = -6,
                                                                    },
                                                                },
                                                        },
                                                    },
                                                },
                                            },
                                        },
                                        buttonsFlow = new FillFlowContainer
                                        {
                                            Anchor = Anchor.CentreRight,
                                            Origin = Anchor.CentreRight,
                                            Direction = FillDirection.Horizontal,
                                            Margin = new MarginPadding { Horizontal = 8 },
                                            AutoSizeAxes = Axes.Both,
                                            Spacing = new Vector2(5),
                                            ChildrenEnumerable = createButtons()
                                                .Select(button =>
                                                    button.With(b =>
                                                    {
                                                        b.Anchor = Anchor.Centre;
                                                        b.Origin = Anchor.Centre;
                                                    })
                                                ),
                                        },
                                        ownerAvatar = new OwnerAvatar
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Size = new Vector2(icon_height),
                                            Margin = new MarginPadding { Right = 8 },
                                            Masking = true,
                                            CornerRadius = 4,
                                            Alpha = ShowItemOwner ? 1 : 0,
                                        },
                                    },
                                },
                            },
                        },
                    },
                    borderContainer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 10,
                        Children = new Drawable[]
                        {
                            new Box // A transparent box that forces the border to be drawn if the panel background is opaque
                            {
                                RelativeSizeAxes = Axes.Both,
                                Alpha = 0,
                                AlwaysPresent = true,
                            },
                        },
                    },
                },
            };
        }

        private IEnumerable<Drawable> createButtons() =>
            new[]
            {
                new CompletionIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Visible = { BindTarget = completed },
                },
                beatmap == null
                    ? Empty()
                        .With(d =>
                        {
                            d.Anchor = Anchor.Centre;
                            d.Origin = Anchor.Centre;
                        })
                    : new PlaylistDownloadButton(beatmap)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                showResultsButton = new GrayButton(FontAwesome.Solid.ChartPie)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(30, 30),
                    Action = () => RequestResults?.Invoke(Item),
                    Alpha = AllowShowingResults ? 1 : 0,
                    TooltipText = "View results",
                },
                editButton = new PlaylistEditButton
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(30, 30),
                    Alpha = AllowEditing ? 1 : 0,
                    Action = () => RequestEdit?.Invoke(Item),
                    TooltipText = Resources.Localisation.Web.CommonStrings.ButtonsEdit,
                },
                removeButton = new PlaylistRemoveButton
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(30, 30),
                    Alpha = AllowDeletion ? 1 : 0,
                    Action = () => RequestDeletion?.Invoke(Item),
                    TooltipText = "Remove from playlist",
                },
            };

        protected override bool OnHover(HoverEvent e)
        {
            if (thumbnail != null)
                thumbnail.Dimmed.Value = true;

            panelBackground.FadeColour(
                OsuColour.Gray(0.7f),
                BeatmapCard.TRANSITION_DURATION,
                Easing.OutQuint
            );
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            if (thumbnail != null)
                thumbnail.Dimmed.Value = false;

            panelBackground.FadeColour(
                OsuColour.Gray(1f),
                BeatmapCard.TRANSITION_DURATION,
                Easing.OutQuint
            );
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (AllowSelection && valid.Value)
                SelectedItem.Value = Model;
            return true;
        }

        public MenuItem[] ContextMenuItems
        {
            get
            {
                List<MenuItem> items = new List<MenuItem>();

                if (beatmapOverlay != null)
                    items.Add(
                        new OsuMenuItem(
                            "Details...",
                            MenuItemType.Standard,
                            () => beatmapOverlay.FetchAndShowBeatmap(Item.Beatmap.OnlineID)
                        )
                    );

                if (beatmap != null)
                {
                    if (
                        beatmaps.QueryBeatmap(b => b.OnlineID == beatmap.OnlineID)
                            is BeatmapInfo local
                        && !local.BeatmapSet.AsNonNull().DeletePending
                    )
                    {
                        var collectionItems = realm
                            .Realm.All<BeatmapCollection>()
                            .OrderBy(c => c.Name)
                            .AsEnumerable()
                            .Select(c => new CollectionToggleMenuItem(c.ToLive(realm), beatmap))
                            .Cast<OsuMenuItem>()
                            .ToList();

                        if (manageCollectionsDialog != null)
                            collectionItems.Add(
                                new OsuMenuItem(
                                    "Manage...",
                                    MenuItemType.Standard,
                                    manageCollectionsDialog.Show
                                )
                            );

                        items.Add(new OsuMenuItem("Collections") { Items = collectionItems });
                    }
                }

                return items.ToArray();
            }
        }

        public partial class PlaylistEditButton : GrayButton
        {
            public PlaylistEditButton()
                : base(FontAwesome.Solid.Edit) { }
        }

        public partial class PlaylistRemoveButton : GrayButton
        {
            public PlaylistRemoveButton()
                : base(FontAwesome.Solid.MinusSquare) { }
        }

        private sealed partial class PlaylistDownloadButton : BeatmapDownloadButton
        {
            private readonly IBeatmapInfo beatmap;

            [Resolved]
            private BeatmapManager beatmapManager { get; set; } = null!;

            // required for download tracking, as this button hides itself. can probably be removed with a bit of consideration.
            public override bool IsPresent => true;

            private const float width = 50;

            public PlaylistDownloadButton(IBeatmapInfo beatmap)
                : base(beatmap.BeatmapSet)
            {
                this.beatmap = beatmap;

                Size = new Vector2(width, 30);
                Alpha = 0;
            }

            protected override void LoadComplete()
            {
                State.BindValueChanged(stateChanged, true);

                // base implementation calls FinishTransforms, so should be run after the above state update.
                base.LoadComplete();
            }

            private void stateChanged(ValueChangedEvent<DownloadState> state)
            {
                switch (state.NewValue)
                {
                    case DownloadState.Unknown:
                        // Ignore initial state to ensure the button doesn't briefly appear.
                        break;

                    case DownloadState.LocallyAvailable:
                        // Perform a local query of the beatmap by beatmap checksum, and reset the state if not matching.
                        if (beatmapManager.QueryBeatmap(b => b.MD5Hash == beatmap.MD5Hash) == null)
                            State.Value = DownloadState.NotDownloaded;
                        else
                        {
                            this.FadeTo(0, 500).ResizeWidthTo(0, 500, Easing.OutQuint);
                        }

                        break;

                    default:
                        this.ResizeWidthTo(width, 500, Easing.OutQuint).FadeTo(1, 500);
                        break;
                }
            }
        }

        // For now, this is the same implementation as in PanelBackground, but supports a beatmap info rather than a working beatmap
        private partial class PanelBackground : Container // todo: should be a buffered container (https://github.com/ppy/osu-framework/issues/3222)
        {
            public readonly Bindable<IBeatmapInfo?> Beatmap = new Bindable<IBeatmapInfo?>();

            public PanelBackground()
            {
                UpdateableBeatmapBackgroundSprite backgroundSprite;

                InternalChildren = new Drawable[]
                {
                    backgroundSprite = new UpdateableBeatmapBackgroundSprite
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new FillFlowContainer
                    {
                        Depth = -1,
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        // This makes the gradient not be perfectly horizontal, but diagonal at a ~40° angle
                        Shear = new Vector2(0.8f, 0),
                        Alpha = 0.6f,
                        Children = new[]
                        {
                            // The left half with no gradient applied
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black,
                                Width = 0.4f,
                            },
                            // Piecewise-linear gradient with 2 segments to make it appear smoother
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientHorizontal(
                                    Color4.Black,
                                    new Color4(0f, 0f, 0f, 0.7f)
                                ),
                                Width = 0.4f,
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientHorizontal(
                                    new Color4(0f, 0f, 0f, 0.7f),
                                    new Color4(0, 0, 0, 0.4f)
                                ),
                                Width = 0.4f,
                            },
                        },
                    },
                };

                // manual binding required as playlists don't expose IBeatmapInfo currently.
                // may be removed in the future if this changes.
                Beatmap.BindValueChanged(beatmap =>
                    backgroundSprite.Beatmap.Value = beatmap.NewValue
                );
            }
        }

        private partial class OwnerAvatar : UpdateableAvatar, IHasTooltip
        {
            public OwnerAvatar()
            {
                AddInternal(new TooltipArea(this) { RelativeSizeAxes = Axes.Both, Depth = -1 });
            }

            public LocalisableString TooltipText =>
                User == null ? string.Empty : $"queued by {User.Username}";

            private partial class TooltipArea : Component, IHasTooltip
            {
                private readonly OwnerAvatar avatar;

                public TooltipArea(OwnerAvatar avatar)
                {
                    this.avatar = avatar;
                }

                public LocalisableString TooltipText => avatar.TooltipText;
            }
        }

        public partial class MainFlow : FillFlowContainer
        {
            private readonly Func<bool> allowInteraction;

            public override bool PropagatePositionalInputSubTree => allowInteraction();

            public MainFlow(Func<bool> allowInteraction)
            {
                this.allowInteraction = allowInteraction;
            }
        }

        private partial class CompletionIcon : CompositeDrawable, IHasTooltip
        {
            public readonly BindableBool Visible = new BindableBool();

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChild = new CircularContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(16),
                    Masking = true,
                    Colour = colours.Lime0,
                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both },
                        new SpriteIcon
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Scale = new Vector2(0.5f),
                            Colour = OsuColour.Gray(0.5f),
                            Icon = FontAwesome.Solid.Check,
                        },
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Visible.BindValueChanged(onVisibleChanged, true);
            }

            private void onVisibleChanged(ValueChangedEvent<bool> visible)
            {
                if (visible.NewValue)
                {
                    Size = new Vector2(16);
                    Alpha = 1;
                }
                else
                {
                    Size = Vector2.Zero;
                    Alpha = 0;
                }
            }

            public LocalisableString TooltipText =>
                DrawableRoomPlaylistItemStrings.CompletedTooltip;
        }
    }
}
