using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System;

namespace SoundManager
{
    public class Media
    {
        private Dictionary<string, SoundEffect> soundEffects;
        private Dictionary<string, Song> songs;
        private ContentManager content;

        public Media(ContentManager content)
        {
            this.content = content;
            soundEffects = new Dictionary<string, SoundEffect>();
            songs = new Dictionary<string, Song>();
        }

        public void LoadSoundEffect(string name)
        {
            Console.WriteLine("Loading sound effect: " + name);
            SoundEffect soundEffect = content.Load<SoundEffect>(name);
            soundEffects[name] = soundEffect;
        }

        public void PlaySoundEffect(string name)
        {
            if (soundEffects.ContainsKey(name))
            {
                soundEffects[name].Play();
            }
        }

        public void LoadSong(string name)
        {
            Song song = content.Load<Song>(name);
            songs[name] = song;
        }

        public void PlaySong(string name)
        {
            if (songs.ContainsKey(name))
            {
                MediaPlayer.Play(songs[name]);
            }
        }

        public static void PauseSong()
        {
            MediaPlayer.Pause();
        }

        public static void StopSong()
        {
            MediaPlayer.Stop();
        }
    }
}