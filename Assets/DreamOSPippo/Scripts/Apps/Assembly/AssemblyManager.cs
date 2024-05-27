using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class AssemblyManager : MonoBehaviour
    {
        public MusicPlayerPlaylist assetsPlaylist;
        public GameObject assetsRoot;
        public GameObject assemblyRoot;
        public GameObject assetPrefab;

        void Start()
        {
            // destroy all children
            foreach (Transform child in assetsRoot.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in assemblyRoot.transform)
            {
                Destroy(child.gameObject);
            }

            // iterate over the assets
            for (int i = 0; i < assetsPlaylist.playlist.Count; i++)
            {
                GameObject asset = Instantiate(assetPrefab, assetsRoot.transform);
                PlaylistTrack track = asset.GetComponent<PlaylistTrack>();
                MusicPlayerPlaylist.MusicItem item = assetsPlaylist.playlist[i];
                asset.name = item.artistTitle + " - " + item.musicTitle;
                track.coverImage.sprite = item.musicCover;
                track.titleText.text = item.musicTitle;
                track.artistText.text = item.artistTitle;
                track.durationText.text = (((int)item.musicClip.length / 60) % 60) + ":" + ((int)item.musicClip.length % 60).ToString("D2");
                AssemblyTrack assemblyTrack = asset.GetComponent<AssemblyTrack>();
                assemblyTrack.ParentLeft = assetsRoot;
                assemblyTrack.ParentRight = assemblyRoot;
                assemblyTrack.AudioClip = item.musicClip;
                assemblyTrack.AssemblyManager = this;
                assemblyTrack.Spectrogram.overrideSprite = item.musicSpectrogram;
                assemblyTrack.Resize(item.musicClip.length);
            }
        }

        void OnEnable()
        {
            StopAllAudio();
        }

        public void RestartAllAudio()
        {
            foreach (AudioSource audioSource in FindObjectsOfType<AudioSource>())
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                    audioSource.Play();
                }
            }
        }

        public void StopAllAudio()
        {
            foreach (AudioSource audioSource in FindObjectsOfType<AudioSource>())
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }

        public void TryToExport()
        {
            bool hasCorrectGuitars = false;
            bool hasCorrectPerc = false;
            bool hasCorrectVoice = false;
            bool hasCorrectBass = false;

            foreach (Transform child in assemblyRoot.transform)
            {

                AssemblyTrack track = child.GetComponent<AssemblyTrack>();

                if (track.AudioClip.name.Contains("wrong"))
                {
                    //Debug.LogWarning("Wrong Audio Clip!");
                    PippoChat.Instance.CreateNotification("Assembly Result", "Something sounds off ...");
                    return;
                }
                else if (track.AudioClip.name.Contains("correct_guitars"))
                {
                    hasCorrectGuitars = true;
                }
                else if (track.AudioClip.name.Contains("correct_perc"))
                {
                    hasCorrectPerc = true;
                }
                else if (track.AudioClip.name.Contains("correct_voice"))
                {
                    hasCorrectVoice = true;
                }
                else if (track.AudioClip.name.Contains("correct_bass"))
                {
                    hasCorrectBass = true;
                }
            }

            if (hasCorrectGuitars && hasCorrectPerc && hasCorrectVoice && hasCorrectBass)
            {
                // Debug.Log("All is correct!");
                PippoChat.Instance.CreateNotification("Assembly Result", "That sounds great!");
                return;
            }
            else
            {
                // Debug.LogWarning("Something is missing!");
                PippoChat.Instance.CreateNotification("Assembly Result", "Something is missing.");
                return;
            }
        }

    }

}