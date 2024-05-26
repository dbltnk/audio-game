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
        yield return new WaitForSeconds(3.0f);
        notificationManager.popupDuration = 7.0f;
        notificationManager.CreateNotification(mailIcon, "New Email", "Claire Salisbury: Vince, we've given you access ...");
        yield return new WaitForSeconds(3.0f);
        messagingManager.CreateStoryTeller("PavelTest", "Pavel_00");
    }

    public void CreateNotification(string title, string content)
    {
        notificationManager.CreateNotification(defaultIcon, title, content);
    }
}