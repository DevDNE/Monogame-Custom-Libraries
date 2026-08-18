using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonoGame.GameFramework.Audio;

/// <summary>
/// Name-keyed sound effects and songs, with the three volume levers every
/// shipped game needs and nothing this repo has evidence for beyond them.
///
/// Before this there was no volume anywhere in the engine: every effect played
/// at full amplitude, MediaPlayer kept whatever the OS handed it, and
/// SettingsManager persisted window geometry only. That is not a missing
/// feature so much as a missing options screen.
/// </summary>
public class SoundManager
{
    private readonly Dictionary<string, SoundEffect> soundEffects = new();
    private readonly Dictionary<string, Song> songs = new();
    private readonly List<SoundEffectInstance> loops = new();
    private readonly Dictionary<SoundEffectInstance, float> requestedVolumes = new();
    private ContentManager content;
    private bool songPlaying;

    private float masterVolume = 1f;
    private float soundVolume = 1f;
    private float musicVolume = 1f;

    /// <summary>Scales both categories. 0 is silence, 1 is unattenuated.</summary>
    public float MasterVolume
    {
        get => masterVolume;
        set { masterVolume = MathHelper.Clamp(value, 0f, 1f); ApplyVolumes(); }
    }

    /// <summary>Scales sound effects only.</summary>
    public float SoundVolume
    {
        get => soundVolume;
        set { soundVolume = MathHelper.Clamp(value, 0f, 1f); ApplyVolumes(); }
    }

    /// <summary>Scales songs only.</summary>
    public float MusicVolume
    {
        get => musicVolume;
        set { musicVolume = MathHelper.Clamp(value, 0f, 1f); ApplyVolumes(); }
    }

    /// <summary>
    /// What a call actually plays at: master, then the category, then whatever
    /// the call site asked for.
    ///
    /// Static and pure so the mixing can be asserted without an audio device --
    /// which is the whole of what is worth asserting here, since the rest of
    /// this type is a dictionary lookup and a call into XNA.
    /// </summary>
    public static float EffectiveVolume(float master, float category, float requested)
        => MathHelper.Clamp(master, 0f, 1f)
         * MathHelper.Clamp(category, 0f, 1f)
         * MathHelper.Clamp(requested, 0f, 1f);

    public void LoadContent(ContentManager content)
    {
        this.content = content;
    }

    public void LoadSoundEffect(string name)
    {
        // Guarded the same way PlaySoundEffect is. Half of this type used to be
        // defensive about missing setup and the other half dereferenced a null
        // ContentManager.
        if (!EnsureContent(nameof(LoadSoundEffect), name)) return;
        SoundEffect soundEffect = content.Load<SoundEffect>(name);
        soundEffects[name] = soundEffect;
    }

    /// <param name="volume">Per-call level before master and category scaling.</param>
    /// <param name="pitch">-1 to 1, an octave either way. A few hundredths of
    /// random variation is what stops a sound fired many times a second from
    /// fusing into one flat tone.</param>
    /// <param name="pan">-1 hard left to 1 hard right.</param>
    public void PlaySoundEffect(string name, float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        if (soundEffects.TryGetValue(name, out SoundEffect effect))
        {
            effect.Play(
                EffectiveVolume(masterVolume, soundVolume, volume),
                MathHelper.Clamp(pitch, -1f, 1f),
                MathHelper.Clamp(pan, -1f, 1f));
            return;
        }
        Debug.WriteLine($"[SoundManager] PlaySoundEffect('{name}') called before LoadSoundEffect. No-op.");
    }

    /// <summary>
    /// Start a looping effect and hand back its instance so the caller can stop
    /// it. A looping sound is the one case a fire-and-forget Play cannot serve:
    /// there is no handle to stop, and it never ends on its own.
    /// </summary>
    public SoundEffectInstance PlayLooping(string name, float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        if (!soundEffects.TryGetValue(name, out SoundEffect effect))
        {
            Debug.WriteLine($"[SoundManager] PlayLooping('{name}') called before LoadSoundEffect. No-op.");
            return null;
        }

        SoundEffectInstance instance = effect.CreateInstance();
        instance.IsLooped = true;
        instance.Volume = EffectiveVolume(masterVolume, soundVolume, volume);
        instance.Pitch = MathHelper.Clamp(pitch, -1f, 1f);
        instance.Pan = MathHelper.Clamp(pan, -1f, 1f);
        instance.Play();

        // Tracked so a volume change reaches sounds already playing. A one-shot
        // needs no tracking because it is over before a slider can move.
        loops.Add(instance);
        requestedVolumes[instance] = MathHelper.Clamp(volume, 0f, 1f);
        return instance;
    }

    /// <summary>Stop and forget every looping effect started through <see cref="PlayLooping"/>.</summary>
    public void StopLoops()
    {
        foreach (SoundEffectInstance instance in loops)
        {
            instance.Stop();
            instance.Dispose();
        }
        loops.Clear();
        requestedVolumes.Clear();
    }

    public void LoadSong(string name)
    {
        if (!EnsureContent(nameof(LoadSong), name)) return;
        Song song = content.Load<Song>(name);
        songs[name] = song;
    }

    public void PlaySong(string name, bool looping = false)
    {
        if (songs.TryGetValue(name, out Song song))
        {
            MediaPlayer.IsRepeating = looping;
            MediaPlayer.Volume = EffectiveVolume(masterVolume, musicVolume, 1f);
            MediaPlayer.Play(song);
            songPlaying = true;
            return;
        }
        Debug.WriteLine($"[SoundManager] PlaySong('{name}') called before LoadSong. No-op.");
    }

    private bool EnsureContent(string caller, string assetName)
    {
        if (content != null) return true;
        Debug.WriteLine($"[SoundManager] {caller}('{assetName}') called before LoadContent. No-op.");
        return false;
    }

    /// <summary>
    /// Push the current levels at everything already playing.
    ///
    /// MediaPlayer is only touched once a song has actually started: its
    /// properties reach into the platform audio stack, and the volume setters
    /// are the ones an options screen calls, which can happen long before any
    /// audio device exists -- during boot, or in a test.
    /// </summary>
    private void ApplyVolumes()
    {
        if (songPlaying) MediaPlayer.Volume = EffectiveVolume(masterVolume, musicVolume, 1f);

        foreach (SoundEffectInstance instance in loops)
        {
            float requested = requestedVolumes.TryGetValue(instance, out float r) ? r : 1f;
            instance.Volume = EffectiveVolume(masterVolume, soundVolume, requested);
        }
    }

    public void PauseSong() => MediaPlayer.Pause();

    public void StopSong()
    {
        MediaPlayer.Stop();
        songPlaying = false;
    }
}
