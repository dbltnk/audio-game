using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class AssemblyTrack : MonoBehaviour
{
    public GameObject ParentLeft;
    public GameObject ParentRight;
    public AudioSource AudioSource;
    public AudioClip AudioClip;
    public Michsky.DreamOS.AssemblyManager AssemblyManager;
    public Image Spectrogram;

    void Start()
    {
        AudioSource = GetComponent<AudioSource>();
    }

    public void Resize(float durationInSeconds)
    {
        // Max length of all tracks is 3:17 (197 seconds)
        // Scale the Spectrogram to the correct width
        // It should be the current (full) wdith if durationInSeconds is 197
        // And it should be 0 if durationInSeconds is 0
        Spectrogram.rectTransform.sizeDelta = new Vector2((durationInSeconds / 197) * 400, Spectrogram.rectTransform.sizeDelta.y);
    }

    public void TogglePosition()
    {
        AudioSource.clip = AudioClip;

        if (transform.parent == ParentLeft.transform)
        {
            transform.SetParent(ParentRight.transform);
            SortChildrenByName(ParentRight.transform);
            AssemblyManager.RestartAllAudio();
            AudioSource.Play();
        }
        else
        {
            transform.SetParent(ParentLeft.transform);
            SortChildrenByName(ParentLeft.transform);
            AudioSource.Stop();
        }
    }

    private void SortChildrenByName(Transform parent)
    {
        var children = parent.Cast<Transform>().OrderBy(t => t.name).ToList();
        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
        }
    }
}
