using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.DreamOS;
using UnityEngine.UI;

public class PippoChat : MonoBehaviour
{
    public Sprite icon;
    public MessagingManager messagingManager;
    public NotificationManager notificationManager;

    public void Start()
    {
        StartCoroutine(StartChat());
    }

    IEnumerator StartChat()
    {
        yield return new WaitForSeconds(3.0f);
        notificationManager.popupDuration = 7.0f;
        notificationManager.CreateNotification(icon, "New Email", "Claire Salisbury: Vince, we've given you access ...");
        yield return new WaitForSeconds(3.0f);
        messagingManager.CreateStoryTeller("PavelTest", "Pavel_00");
    }
}
