// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework;
using osu.Framework.Configuration;
using osu.Framework.Configuration.Tracking;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Platform;
using YouTubePlayerEX.App.Localisation;

namespace YouTubePlayerEX.App.Config
{
    public class AudioEffectsConfigManager : IniConfigManager<AudioEffectsSetting>
    {
        internal const string FILENAME = @"audio_effects.ini";

        protected override string Filename => FILENAME;

        protected override void InitialiseDefaults()
        {
            SetDefault(AudioEffectsSetting.ReverbEnabled, false);
            SetDefault(AudioEffectsSetting.ReverbWetMix, 1f, 0f, 3f, 0.01f);
            SetDefault(AudioEffectsSetting.ReverbRoomSize, 0.5f, 0f, 1f, 0.01f);
            SetDefault(AudioEffectsSetting.ReverbDamp, 0.5f, 0f, 1f, 0.01f);
            SetDefault(AudioEffectsSetting.ReverbStereoWidth, 1f, 0f, 1f, 0.01f);

            SetDefault(AudioEffectsSetting.RotateEnabled, false);
            SetDefault(AudioEffectsSetting.RotateRate, 1f, 0f, 1f, 0.01f);

            SetDefault(AudioEffectsSetting.EchoEnabled, false);
            SetDefault(AudioEffectsSetting.EchoDryMix, 2f, 0f, 4f, 0.01f);
            SetDefault(AudioEffectsSetting.EchoWetMix, 2f, 0f, 4f, 0.01f);
            SetDefault(AudioEffectsSetting.EchoFeedback, 1f, 0f, 2f, 0.01f);
            SetDefault(AudioEffectsSetting.EchoDelay, 0f, 0f, 6f, 0.01f);

            SetDefault(AudioEffectsSetting.DistortionEnabled, false);
            SetDefault(AudioEffectsSetting.DistortionVolume, 0.3f, 0f, 2f, 0.01f);
            SetDefault(AudioEffectsSetting.DistortionDrive, 0f, 0f, 5f, 0.1f);

            SetDefault(AudioEffectsSetting.KaraokeEnabled, false);
        }

        public AudioEffectsConfigManager(Storage storage, IDictionary<AudioEffectsSetting, object> defaultOverrides = null) : base(storage, defaultOverrides)
        {
        }
    }

    public enum AudioEffectsSetting
    {
        //reverb
        ReverbEnabled,
        ReverbWetMix,
        ReverbRoomSize,
        ReverbDamp,
        ReverbStereoWidth,

        //rotate
        RotateEnabled,
        RotateRate,

        //echo
        EchoEnabled,
        EchoDryMix,
        EchoWetMix,
        EchoFeedback,
        EchoDelay,

        //distortion
        DistortionEnabled,
        DistortionVolume,
        DistortionDrive,

        //karaoke (what else)
        KaraokeEnabled
    }
}
