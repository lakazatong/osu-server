﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.App;
using Android.Content.PM;
using Microsoft.Maui.Devices;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Screens;
using osu.Game.Updater;
using osu.Game.Utils;
using osuTK;

namespace osu.Android
{
    public partial class OsuGameAndroid : OsuGame
    {
        [Cached]
        private readonly OsuGameActivity gameActivity;

        public override Vector2 ScalingContainerTargetDrawSize =>
            new Vector2(1024, 1024 * DrawHeight / DrawWidth);

        public OsuGameAndroid(OsuGameActivity activity)
            : base(null)
        {
            gameActivity = activity;
        }

        public override Version AssemblyVersion
        {
            get
            {
                var packageInfo = Application
                    .Context.ApplicationContext!.PackageManager!.GetPackageInfo(
                        Application.Context.ApplicationContext.PackageName!,
                        0
                    )
                    .AsNonNull();

                try
                {
                    // We store the osu! build number in the "VersionCode" field to better support google play releases.
                    // If we were to use the main build number, it would require a new submission each time (similar to TestFlight).
                    // In order to do this, we should split it up and pad the numbers to still ensure sequential increase over time.
                    //
                    // We also need to be aware that older SDK versions store this as a 32bit int.
                    //
                    // Basic conversion format (as done in Fastfile): 2020.606.0 -> 202006060

                    // https://stackoverflow.com/questions/52977079/android-sdk-28-versioncode-in-packageinfo-has-been-deprecated
                    string versionName;

                    if (OperatingSystem.IsAndroidVersionAtLeast(28))
                    {
                        versionName = packageInfo.LongVersionCode.ToString();
                        // ensure we only read the trailing portion of long (the part we are interested in).
                        versionName = versionName.Substring(versionName.Length - 9);
                    }
                    else
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        // this is required else older SDKs will report missing method exception.
                        versionName = packageInfo.VersionCode.ToString();
#pragma warning restore CS0618 // Type or member is obsolete
                    }

                    // undo play store version garbling (as mentioned above).
                    return new Version(
                        int.Parse(versionName.Substring(0, 4)),
                        int.Parse(versionName.Substring(4, 4)),
                        int.Parse(versionName.Substring(8, 1))
                    );
                }
                catch { }

                return new Version(packageInfo.VersionName.AsNonNull());
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            UserPlayingState.BindValueChanged(_ => updateOrientation());
        }

        protected override void ScreenChanged(IOsuScreen? current, IOsuScreen? newScreen)
        {
            base.ScreenChanged(current, newScreen);

            if (newScreen != null)
                updateOrientation();
        }

        private void updateOrientation()
        {
            var orientation = MobileUtils.GetOrientation(
                this,
                (IOsuScreen)ScreenStack.CurrentScreen,
                gameActivity.IsTablet
            );

            switch (orientation)
            {
                case MobileUtils.Orientation.Locked:
                    gameActivity.RequestedOrientation = ScreenOrientation.Locked;
                    break;

                case MobileUtils.Orientation.Portrait:
                    gameActivity.RequestedOrientation = ScreenOrientation.Portrait;
                    break;

                case MobileUtils.Orientation.Default:
                    gameActivity.RequestedOrientation = gameActivity.DefaultOrientation;
                    break;
            }
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);
            host.Window.CursorState |= CursorState.Hidden;
        }

        protected override UpdateManager CreateUpdateManager() => new MobileUpdateNotifier();

        protected override BatteryInfo CreateBatteryInfo() => new AndroidBatteryInfo();

        private class AndroidBatteryInfo : BatteryInfo
        {
            public override double? ChargeLevel => Battery.ChargeLevel;

            public override bool OnBattery => Battery.PowerSource == BatteryPowerSource.Battery;
        }
    }
}
