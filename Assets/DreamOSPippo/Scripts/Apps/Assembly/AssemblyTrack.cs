using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AssemblyTrack : MonoBehaviour
{
    public GameObject ParentLeft;
    public GameObject ParentRight;
    public AudioSource AudioSource;
    public AudioClip AudioClip;
    public Michsky.DreamOS.AssemblyManager AssemblyManager;

    void Start()
    {
        AudioSource = GetComponent<AudioSource>();
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
