// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Video;
using osu.Framework.Timing;
using osuTK.Graphics;
using YoutubeExplode.Videos.ClosedCaptions;
using NekoPlayer.App.Config;
using NekoPlayer.App.Graphics.Caption;
using NekoPlayer.App.Graphics.Containers;
using NekoPlayer.App.Graphics.Shaders.New;
using NekoPlayer.App.Graphics.Shaders.New.Bloom;
using NekoPlayer.App.Graphics.Shaders.New.Chromatic;
using NekoPlayer.App.Graphics.Shaders.New.Grayscale;
using NekoPlayer.App.Graphics.Shaders.New.HueShift;
using NekoPlayer.App.Online;

namespace NekoPlayer.App.Graphics.Videos
{
    public partial class YouTubeVideoPlayer : Container
    {
        private Video video = null!;
        private Track track = null!;
        private DrawableTrack drawableTrack = null!;
        private Google.Apis.YouTube.v3.Data.Video videoData = null!;

        private string fileName_Video, fileName_Audio = null!;
        private ClosedCaptionTrack captionTrack = null!;
        private ClosedCaptionLanguage captionLanguage;

        private StopwatchClock rateAdjustClock = null!;
        private DecouplingFramedClock framedClock = null!;

        private Bindable<double> playbackSpeed = null!;
        private double resumeFromTime;
        private bool trackFinished = false;

        public Action? OnVideoCompleted = null!;

        private MediaSessionControls mediaSessionControls = null!;

        [Resolved]
        private YouTubeAPI api { get; set; }

#nullable enable
        [Resolved(canBeNull: true)]
        private MediaSession? mediaSession { get; set; }
#nullable disable

        public YouTubeVideoPlayer(string fileName_Video, string fileName_Audio, ClosedCaptionTrack captionTrack, Google.Apis.YouTube.v3.Data.Video videoData, double resumeFromTime)
        {
            this.fileName_Video = fileName_Video;
            this.fileName_Audio = fileName_Audio;
            this.captionTrack = captionTrack;
            this.videoData = videoData;
            this.resumeFromTime = resumeFromTime;
        }

        public void UpdateCaptionTrack(ClosedCaptionTrack captionTrack)
        {
            this.captionTrack = captionTrack;
            closedCaption.UpdateCaptionTrack(captionLanguage, captionTrack);
        }

        public BindableNumber<double> VideoProgress = new BindableNumber<double>()
        {
            MinValue = 0,
            MaxValue = 1,
        };

        private KeyBindingAnimations keyBindingAnimations = null!;
        private ClosedCaptionContainer closedCaption = null!;
        private Bindable<AspectRatioMethod> aspectRatioMethod = null!;
        private Bindable<float> videoBloomLevel, chromaticAberrationStrength, videoGrayscaleLevel, videoHueShift = null!;

        private VideoNewShaderContainer bloom, chromatic, grayscale, hueShift = null!;

        [BackgroundDependencyLoader]
        private void load(ITrackStore tracks, NekoPlayerConfigManager config, ScreenshotManager screenshotManager)
        {
            uiVisible = screenshotManager.CursorVisibility.GetBoundCopy();
            aspectRatioMethod = config.GetBindable<AspectRatioMethod>(NekoPlayerSetting.AspectRatioMethod);
            videoBloomLevel = config.GetBindable<float>(NekoPlayerSetting.VideoBloomLevel);
            videoGrayscaleLevel = config.GetBindable<float>(NekoPlayerSetting.VideoGrayscaleLevel);
            videoHueShift = config.GetBindable<float>(NekoPlayerSetting.VideoHueShift);
            chromaticAberrationStrength = config.GetBindable<float>(NekoPlayerSetting.ChromaticAberrationStrength);
            track = tracks.GetFromStream(File.OpenRead(fileName_Audio), fileName_Audio);
            playbackSpeed = new Bindable<double>(1);

            rateAdjustClock = new StopwatchClock(false);
            framedClock = new DecouplingFramedClock(rateAdjustClock);

            mediaSessionControls = new MediaSessionControls()
            {
                NextButtonPressed = FastForward10Sec,
                PrevButtonPressed = FastRewind10Sec,
                PlayButtonPressed = () => Play(),
                PauseButtonPressed = () => Pause(),
                OnSeek = pos =>
                {
                    SeekTo(pos);
                },
            };

            mediaSession?.CreateMediaSession(api, fileName_Audio);

            mediaSession?.RegisterControlEvents(mediaSessionControls);

            AddRange(new Drawable[] {
                drawableTrack = new DrawableTrack(track)
                {
                    Clock = framedClock,
                },
                new ScalingContainerNew(ScalingMode.Video)
                {
                    Children = new Drawable[] {
                        new DimmableContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = grayscale = new GrayscaleContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = chromatic = new ChromaticContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = bloom = new BloomContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Child = hueShift = new HueShiftContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = Color4.Black,
                                                },
                                                video = new Video(fileName_Video, false)
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    FillMode = FillMode.Fit,
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Clock = framedClock,
                                                }
                                            }
                                        }
                                    },
                                }
                            }
                        },
                        keyBindingAnimations = new KeyBindingAnimations
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                    }
                },
                closedCaption = new ClosedCaptionContainer(this, captionTrack)
            });

            rateAdjustClock.Rate = playbackSpeed.Value;

            playbackSpeed.BindValueChanged(v =>
            {
                rateAdjustClock.Rate = v.NewValue;
                mediaSession?.UpdatePlaybackSpeed(v.NewValue);
            });

            UpdatePreservePitch(config.Get<bool>(NekoPlayerSetting.AdjustPitchOnSpeedChange));

            SeekTo(resumeFromTime * 1000);
            Play();

            uiVisible.BindValueChanged(visible =>
            {
                Schedule(() =>
                {
                    if (visible.NewValue)
                    {
                        keyBindingAnimations.Show();
                        closedCaption.Show();
                    }
                    else
                    {
                        keyBindingAnimations.Hide();
                        closedCaption.Hide();
                    }
                });
            }, true);

            aspectRatioMethod.BindValueChanged(value =>
            {
                video.FillMode = value.NewValue == AspectRatioMethod.Letterbox ? FillMode.Fit : FillMode.Stretch;
            }, true);

            videoBloomLevel.BindValueChanged(value =>
            {
                bloom.Strength = value.NewValue;
            }, true);

            videoGrayscaleLevel.BindValueChanged(value =>
            {
                grayscale.Strength = value.NewValue;
            }, true);

            chromaticAberrationStrength.BindValueChanged(value =>
            {
                chromatic.Strength = value.NewValue;
            }, true);

            videoHueShift.BindValueChanged(value =>
            {
                hueShift.Strength = value.NewValue / 360;
            }, true);

            drawableTrack.Completed += trackCompleted;
        }

        private IBindable<bool> uiVisible = null!;

        private void trackCompleted()
        {
            trackFinished = true;
            SeekTo(0);
            Pause();
            OnVideoCompleted?.Invoke();
            /*
            drawableTrack?.Stop();
            framedClock.Stop();
            SeekTo(0);
            */ // fix app freezing on track completed
        }

        public void UpdatePreservePitch(bool value)
        {
            drawableTrack?.RemoveAllAdjustments(AdjustableProperty.Tempo);
            drawableTrack?.RemoveAllAdjustments(AdjustableProperty.Frequency);

            if (value == true)
                drawableTrack?.AddAdjustment(AdjustableProperty.Frequency, playbackSpeed);
            else
                drawableTrack?.AddAdjustment(AdjustableProperty.Tempo, playbackSpeed);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            mediaSession?.UnregisterControlEvents();
            mediaSession?.DeleteMediaSession();

            drawableTrack.Dispose();
            video.Dispose();
        }

        public bool IsPlaying()
        {
            if (drawableTrack == null)
                return false;

            if (drawableTrack.HasCompleted)
                return false;

            return drawableTrack.IsRunning;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            mediaSession?.UpdateMediaSession(videoData);
            mediaSession?.UpdateTimestamp(videoData, 0);
        }

        protected override void Update()
        {
            base.Update();

            if (drawableTrack != null)
            {
                VideoProgress.MaxValue = drawableTrack.Length / 1000;
                VideoProgress.Value = drawableTrack.CurrentTime / 1000;
            }
        }

        public void SeekTo(double pos)
        {
            double pos2 = pos;

            if (pos2 < 0)
                pos2 = 0;

            if (drawableTrack != null)
            {
                drawableTrack?.Seek(pos2);
                video?.Seek(pos2);
                mediaSession?.UpdateTimestamp(videoData, pos2);
            }
        }

        public void FastForward10Sec()
        {
            if (drawableTrack != null)
            {
                if ((drawableTrack.CurrentTime + 5000) >= drawableTrack.Length)
                {
                    SeekTo(drawableTrack.Length);
                    trackFinished = true;
                    Pause();
                }

                SeekTo(drawableTrack.CurrentTime + 5000);
                keyBindingAnimations.PlaySeekAnimation(KeyBindingAnimations.SeekAction.FastForward10sec, FontAwesome.Solid.Box);
                mediaSession?.UpdateTimestamp(videoData, drawableTrack.CurrentTime);
            }
        }

        public void FastRewind10Sec()
        {
            if (drawableTrack != null)
            {
                SeekTo(drawableTrack.CurrentTime - 5000);
                keyBindingAnimations.PlaySeekAnimation(KeyBindingAnimations.SeekAction.FastRewind10sec, FontAwesome.Solid.Box);
                mediaSession?.UpdateTimestamp(videoData, drawableTrack.CurrentTime);
            }
        }

        public void Pause(bool isKeyboardAction = false)
        {
            if (drawableTrack != null)
            {
                drawableTrack?.Stop();
                framedClock.Stop();

                mediaSession?.UpdatePlayingState(false);
                mediaSession?.UpdateTimestamp(videoData, drawableTrack.CurrentTime);

                if (isKeyboardAction)
                    keyBindingAnimations.PlaySeekAnimation(KeyBindingAnimations.SeekAction.PlayPause, FontAwesome.Solid.Pause);
            }
        }

        public void Play(bool isKeyboardAction = false)
        {
            if (drawableTrack != null)
            {
                if (trackFinished)
                {
                    if (drawableTrack.CurrentTime == drawableTrack.Length)
                        SeekTo(0);

                    trackFinished = false;
                }

                mediaSession?.UpdatePlayingState(true);
                mediaSession?.UpdateTimestamp(videoData, drawableTrack.CurrentTime);

                drawableTrack?.Start();
                framedClock.Start();

                if (isKeyboardAction)
                    keyBindingAnimations.PlaySeekAnimation(KeyBindingAnimations.SeekAction.PlayPause, FontAwesome.Solid.Play);
            }
        }

        [Resolved]
        private SessionStatics sessionStatics { get; set; } = null!;

        public void SetPlaybackSpeed(double speed)
        {
            playbackSpeed.Value = speed;
        }
    }
}
