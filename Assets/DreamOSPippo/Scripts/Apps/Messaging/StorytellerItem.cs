using UnityEngine;

namespace Michsky.DreamOS
{
    [RequireComponent(typeof(ButtonManager))]
    public class StorytellerItem : MonoBehaviour
    {
        [HideInInspector] public int itemIndex;
        [HideInInspector] public int layoutIndex;
        [HideInInspector] public ChatLayoutPreset layout;
        [HideInInspector] public MessagingManager msgManager;
        [HideInInspector] public DynamicMessageHandler handler;
        [HideInInspector] public string replyLocKey;

        void Start()
        {
            ButtonManager strButton = gameObject.GetComponent<ButtonManager>();
            strButton.onClick.AddListener(delegate
            {
                msgManager.HideStorytellerPanel();

                var reply = msgManager.chatList[layoutIndex].chatAsset.storyTeller[msgManager.storyTellerIndex].replies[itemIndex];

                // Always send the text message first
                string tempMsg = !string.IsNullOrEmpty(replyLocKey) ? replyLocKey : reply.replyContent;
                msgManager.CreateMessage(layout, tempMsg);

                // Then, if there's media, send it as a separate message
                switch (reply.objectType)
                {
                    case MessagingChat.ObjectType.AudioMessage:
                        msgManager.CreateAudioMessage(layout, reply.audioMessage);
                        break;
                    case MessagingChat.ObjectType.ImageMessage:
                        msgManager.CreateImageMessage(layout, reply.imageMessage, "Image Reply", "");
                        break;
                }

                msgManager.stItemIndex = itemIndex;
                msgManager.isStoryTellerOpen = false;

                handler.StartCoroutine(handler.HandleStoryTellerLatency(reply.feedbackLatency, layoutIndex, itemIndex));

                for (int i = 0; i < msgManager.storytellerReplyEvents.Count; ++i)
                {
                    if (msgManager.storytellerReplyEvents[i].replyID == gameObject.name)
                    {
                        msgManager.storytellerReplyEvents[i].onReplySelect.Invoke();
                        break;
                    }
                }
            });
        }
    }
}