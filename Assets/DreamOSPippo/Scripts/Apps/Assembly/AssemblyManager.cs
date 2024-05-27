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
            PippoChat.Instance.CreateNotification("Assembly Manager", "Assembly Manager has been activated.");
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

    }

}