using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AssemblyTrack : MonoBehaviour
{
    public GameObject ParentLeft;
    public GameObject ParentRight;

    public void TogglePosition()
    {
        // change the parent object
        if (transform.parent == ParentLeft.transform)
        {
            transform.SetParent(ParentRight.transform);
            SortChildrenByName(ParentRight.transform);
        }
        else
        {
            transform.SetParent(ParentLeft.transform);
            SortChildrenByName(ParentLeft.transform);
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
