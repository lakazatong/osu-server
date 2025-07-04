// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using MessagePack;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;

namespace osu.Game.Tests.Online
{
    [TestFixture]
    public class TestAPIModMessagePackSerialization
    {
        [Test]
        public void TestAcronymIsPreserved()
        {
            var apiMod = new APIMod(new TestMod());

            var deserialized = MessagePackSerializer.Deserialize<APIMod>(
                MessagePackSerializer.Serialize(apiMod)
            );

            Assert.That(deserialized.Acronym, Is.EqualTo(apiMod.Acronym));
        }

        [Test]
        public void TestRawSettingIsPreserved()
        {
            var apiMod = new APIMod(new TestMod { TestSetting = { Value = 2 } });

            var deserialized = MessagePackSerializer.Deserialize<APIMod>(
                MessagePackSerializer.Serialize(apiMod)
            );

            Assert.That(deserialized.Settings, Contains.Key("test_setting").With.ContainValue(2.0));
        }

        [Test]
        public void TestConvertedModHasCorrectSetting()
        {
            var apiMod = new APIMod(new TestMod { TestSetting = { Value = 2 } });

            var deserialized = MessagePackSerializer.Deserialize<APIMod>(
                MessagePackSerializer.Serialize(apiMod)
            );
            var converted = (TestMod)deserialized.ToMod(new TestRuleset());

            Assert.That(converted.TestSetting.Value, Is.EqualTo(2));
        }

        [Test]
        public void TestDeserialiseTimeRampMod()
        {
            // Create the mod with values different from default.
            var apiMod = new APIMod(
                new TestModTimeRamp
                {
                    AdjustPitch = { Value = false },
                    InitialRate = { Value = 1.25 },
                    FinalRate = { Value = 0.25 },
                }
            );

            var deserialised = MessagePackSerializer.Deserialize<APIMod>(
                MessagePackSerializer.Serialize(apiMod)
            );
            var converted = (TestModTimeRamp)deserialised.ToMod(new TestRuleset());

            Assert.That(converted.AdjustPitch.Value, Is.EqualTo(false));
            Assert.That(converted.InitialRate.Value, Is.EqualTo(1.25));
            Assert.That(converted.FinalRate.Value, Is.EqualTo(0.25));
        }

        [Test]
        public void TestDeserialiseEnumMod()
        {
            var apiMod = new APIMod(new TestModEnum { TestSetting = { Value = TestEnum.Value2 } });

            var deserialized = MessagePackSerializer.Deserialize<APIMod>(
                MessagePackSerializer.Serialize(apiMod)
            );

            Assert.That(deserialized.Settings, Contains.Key("test_setting").With.ContainValue(1));
        }

        private class TestRuleset : Ruleset
        {
            public override IEnumerable<Mod> GetModsFor(ModType type) =>
                new Mod[] { new TestMod(), new TestModTimeRamp() };

            public override DrawableRuleset CreateDrawableRulesetWith(
                IBeatmap beatmap,
                IReadOnlyList<Mod> mods = null
            ) => throw new System.NotImplementedException();

            public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) =>
                throw new System.NotImplementedException();

            public override DifficultyCalculator CreateDifficultyCalculator(
                IWorkingBeatmap beatmap
            ) => throw new System.NotImplementedException();

            public override string Description { get; } = string.Empty;
            public override string ShortName { get; } = string.Empty;
        }

        private class TestMod : Mod
        {
            public override string Name => "Test Mod";
            public override string Acronym => "TM";
            public override LocalisableString Description => "This is a test mod.";
            public override double ScoreMultiplier => 1;

            [SettingSource("Test")]
            public BindableNumber<double> TestSetting { get; } =
                new BindableDouble
                {
                    MinValue = 0,
                    MaxValue = 10,
                    Default = 5,
                    Precision = 0.01,
                };
        }

        private class TestModTimeRamp : ModTimeRamp
        {
            public override string Name => "Test Mod";
            public override string Acronym => "TMTR";
            public override LocalisableString Description => "This is a test mod.";
            public override double ScoreMultiplier => 1;

            [SettingSource("Initial rate", "The starting speed of the track")]
            public override BindableNumber<double> InitialRate { get; } =
                new BindableDouble(1.5)
                {
                    MinValue = 1,
                    MaxValue = 2,
                    Precision = 0.01,
                };

            [SettingSource("Final rate", "The speed increase to ramp towards")]
            public override BindableNumber<double> FinalRate { get; } =
                new BindableDouble(0.5)
                {
                    MinValue = 0,
                    MaxValue = 1,
                    Precision = 0.01,
                };

            [SettingSource("Adjust pitch", "Should pitch be adjusted with speed")]
            public override BindableBool AdjustPitch { get; } = new BindableBool(true);
        }

        private class TestModEnum : Mod
        {
            public override string Name => "Test Mod";
            public override string Acronym => "TM";
            public override LocalisableString Description => "This is a test mod.";
            public override double ScoreMultiplier => 1;

            [SettingSource("Test")]
            public Bindable<TestEnum> TestSetting { get; } = new Bindable<TestEnum>();
        }

        private enum TestEnum
        {
            Value1 = 0,
            Value2 = 1,
            Value3 = 2,
        }
    }
}
