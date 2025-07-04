// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Threading;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UI;
using osu.Game.Skinning;
using osu.Game.Storyboards;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// A more expensive representation of a beatmap which allows access to various associated resources.
    /// - Access textures and other resources via <see cref="GetStream"/>.
    /// - Access the storyboard via <see cref="Storyboard"/>.
    /// - Access a local skin via <see cref="Skin"/>.
    /// - Access the track via <see cref="LoadTrack"/> (and then <see cref="Track"/> for subsequent accesses).
    /// - Create a playable <see cref="Beatmap"/> via <see cref="GetPlayableBeatmap(osu.Game.Rulesets.IRulesetInfo,System.Collections.Generic.IReadOnlyList{osu.Game.Rulesets.Mods.Mod})"/>.
    /// </summary>
    public interface IWorkingBeatmap
    {
        IBeatmapInfo BeatmapInfo { get; }

        /// <summary>
        /// Whether the Beatmap has finished loading.
        ///</summary>
        bool BeatmapLoaded { get; }

        /// <summary>
        /// Whether the Track has finished loading.
        ///</summary>
        bool TrackLoaded { get; }

        /// <summary>
        /// Retrieves the <see cref="IBeatmap"/> which this <see cref="IWorkingBeatmap"/> represents.
        /// </summary>
        IBeatmap Beatmap { get; }

        /// <summary>
        /// Retrieves the background for this <see cref="IWorkingBeatmap"/>.
        /// </summary>
        Texture GetBackground();

        /// <summary>
        /// Retrieves a cropped background for this <see cref="IWorkingBeatmap"/> used for display on panels.
        /// </summary>
        Texture GetPanelBackground();

        /// <summary>
        /// Retrieves the <see cref="Waveform"/> for the <see cref="Track"/> of this <see cref="IWorkingBeatmap"/>.
        /// </summary>
        Waveform Waveform { get; }

        /// <summary>
        /// Retrieves the <see cref="Storyboard"/> which this <see cref="IWorkingBeatmap"/> provides.
        /// </summary>
        Storyboard Storyboard { get; }

        /// <summary>
        /// Retrieves the <see cref="Skin"/> which this <see cref="IWorkingBeatmap"/> provides.
        /// </summary>
        ISkin Skin { get; }

        /// <summary>
        /// Retrieves the <see cref="Track"/> which this <see cref="IWorkingBeatmap"/> has loaded.
        /// </summary>
        Track Track { get; }

        /// <summary>
        /// Constructs a playable <see cref="IBeatmap"/> from <see cref="Beatmap"/> using the applicable converters for a specific <see cref="RulesetInfo"/>.
        /// <para>
        /// The returned <see cref="IBeatmap"/> is in a playable state - all <see cref="HitObject"/> and <see cref="BeatmapDifficulty"/> <see cref="Mod"/>s
        /// have been applied, and <see cref="HitObject"/>s have been fully constructed.
        /// </para>
        /// </summary>
        /// <remarks>
        /// By default, the beatmap load process will be interrupted after 10 seconds.
        /// For finer-grained control over the load process, use the
        /// <see cref="GetPlayableBeatmap(osu.Game.Rulesets.IRulesetInfo,System.Collections.Generic.IReadOnlyList{osu.Game.Rulesets.Mods.Mod},System.Threading.CancellationToken)"/>
        /// overload instead.
        /// </remarks>
        /// <param name="ruleset">The <see cref="RulesetInfo"/> to create a playable <see cref="IBeatmap"/> for.</param>
        /// <param name="mods">The <see cref="Mod"/>s to apply to the <see cref="IBeatmap"/>.</param>
        /// <returns>The converted <see cref="IBeatmap"/>.</returns>
        /// <exception cref="BeatmapInvalidForRulesetException">If <see cref="Beatmap"/> could not be converted to <paramref name="ruleset"/>.</exception>
        IBeatmap GetPlayableBeatmap(IRulesetInfo ruleset, IReadOnlyList<Mod> mods = null);

        /// <summary>
        /// Constructs a playable <see cref="IBeatmap"/> from <see cref="Beatmap"/> using the applicable converters for a specific <see cref="RulesetInfo"/>.
        /// <para>
        /// The returned <see cref="IBeatmap"/> is in a playable state - all <see cref="HitObject"/> and <see cref="BeatmapDifficulty"/> <see cref="Mod"/>s
        /// have been applied, and <see cref="HitObject"/>s have been fully constructed.
        /// </para>
        /// </summary>
        /// <param name="ruleset">The <see cref="RulesetInfo"/> to create a playable <see cref="IBeatmap"/> for.</param>
        /// <param name="mods">The <see cref="Mod"/>s to apply to the <see cref="IBeatmap"/>.</param>
        /// <param name="cancellationToken">Cancellation token that cancels the beatmap loading process.</param>
        /// <returns>The converted <see cref="IBeatmap"/>.</returns>
        /// <exception cref="BeatmapInvalidForRulesetException">If <see cref="Beatmap"/> could not be converted to <paramref name="ruleset"/>.</exception>
        IBeatmap GetPlayableBeatmap(
            IRulesetInfo ruleset,
            IReadOnlyList<Mod> mods,
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Load a new audio track instance for this beatmap. This should be called once before accessing <see cref="Track"/>.
        /// The caller of this method is responsible for the lifetime of the track.
        /// </summary>
        /// <remarks>
        /// In a standard game context, the loading of the track is managed solely by MusicController, which will
        /// automatically load the track of the current global IBindable IWorkingBeatmap.
        /// As such, this method should only be called in very special scenarios, such as external tests or apps which are
        /// outside of the game context.
        /// </remarks>
        /// <returns>A fresh track instance, which will also be available via <see cref="Track"/>.</returns>
        Track LoadTrack();

        /// <summary>
        /// Returns the stream of the file from the given storage path.
        /// </summary>
        /// <param name="storagePath">The storage path to the file.</param>
        Stream GetStream(string storagePath);

        /// <summary>
        /// Beings loading the contents of this <see cref="IWorkingBeatmap"/> asynchronously.
        /// </summary>
        void BeginAsyncLoad();

        /// <summary>
        /// Cancels the asynchronous loading of the contents of this <see cref="IWorkingBeatmap"/>.
        /// </summary>
        void CancelAsyncLoad();

        /// <summary>
        /// Reads the correct track restart point from beatmap metadata and sets looping to enabled.
        /// </summary>
        void PrepareTrackForPreview(bool looping, double offsetFromPreviewPoint = 0);
    }
}
