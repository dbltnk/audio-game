using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.DreamOS;
using UnityEngine.UI;

public class PippoChat : MonoBehaviour
{
    public static PippoChat Instance { get; private set; }

    public Sprite defaultIcon;
    public Sprite mailIcon;
    public MessagingManager messagingManager;
    public NotificationManager notificationManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(StartChat());
    }

    IEnumerator StartChat()
    {
        yield return new WaitForSeconds(1.0f);
        notificationManager.popupDuration = 7.0f;

        // Claire's email
        notificationManager.CreateNotification(mailIcon, "New Email", "Claire Salisbury: Vince, we've given you access ...");

        // Pavel's first message thread
        yield return new WaitForSeconds(5.0f);
        messagingManager.CreateStoryTeller("PavelTest", "Pavel_00");

        // triggers another chat message after a 1m delay
        yield return new WaitForSeconds(60.0f);
        messagingManager.CreateStoryTeller("PavelTest", "Pavel_triggered");
    }

    public void CreateNotification(string title, string content)
    {
        notificationManager.CreateNotification(defaultIcon, title, content);
    }
}