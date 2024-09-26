using System.Collections;
using UnityEngine;

namespace Michsky.DreamOS
{
    public class DynamicMessageHandler : MonoBehaviour
    {
        [HideInInspector] public MessagingManager manager;
        GameObject messageTimerObject;

        public IEnumerator HandleDynamicMessage(float timer, int layoutIndex)
        {
            yield return new WaitForSeconds(timer);

            GameObject timerObj = Instantiate(manager.chatMessageTimer, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            timerObj.transform.SetParent(layout.messageParent, false);

            messageTimerObject = timerObj;
            StartCoroutine(FinishDynamicMessage(manager.chatList[layoutIndex].chatAsset.dynamicMessages[manager.dynamicMessageIndex].replyTimer, layoutIndex));
        }

        IEnumerator FinishDynamicMessage(float timer, int layoutIndex)
        {
            yield return new WaitForSeconds(timer);

            manager.allowInputSubmit = true;
            manager.CreateDynamicMessage(layoutIndex, false);

            Destroy(messageTimerObject);
            Destroy(gameObject);
        }

        public IEnumerator HandleStoryTeller(float timer, int layoutIndex, bool isIndividual)
        {
            yield return new WaitForSeconds(timer);

            GameObject timerObj = Instantiate(manager.chatMessageTimer, new Vector3(0, 0, 0), Quaternion.identity);
            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            timerObj.transform.SetParent(layout.messageParent, false);

            messageTimerObject = timerObj;
            StartCoroutine(CreateStoryTellerMessage(manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].messageTimer, layoutIndex, isIndividual));
        }

        IEnumerator CreateStoryTellerMessage(float timer, int layoutIndex, bool isIndividual)
        {
            yield return new WaitForSeconds(timer);

            Destroy(messageTimerObject);

            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            MessagingChat.StoryTeller storyTeller = manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex];

            switch (storyTeller.objectType)
            {
                case MessagingChat.ObjectType.Message:
                    if (isIndividual)
                        manager.CreateCustomIndividualMessage(layout, storyTeller.messageContent, manager.GetTimeData(), storyTeller.messageKey);
                    else
                        manager.CreateCustomMessage(layout, storyTeller.messageContent, manager.GetTimeData(), storyTeller.messageKey);
                    break;

                case MessagingChat.ObjectType.AudioMessage:
                    if (isIndividual)
                        manager.CreateIndividualAudioMessage(layout, storyTeller.audioMessage, manager.GetTimeData());
                    else
                        manager.CreateAudioMessage(layout, storyTeller.audioMessage, manager.GetTimeData());
                    break;

                case MessagingChat.ObjectType.ImageMessage:
                    if (isIndividual)
                        manager.CreateIndividualImageMessage(layout, storyTeller.imageMessage, "Image from Storyteller", "", manager.GetTimeData());
                    else
                        manager.CreateImageMessage(layout, storyTeller.imageMessage, "Image from Storyteller", "", manager.GetTimeData());
                    break;
            }

            if (manager.stIndexHelper == manager.currentLayout && manager.storyTellerAnimator.gameObject.activeInHierarchy)
            {
                manager.ShowStorytellerPanel();
            }
            manager.isStoryTellerOpen = true;
        }

        public IEnumerator HandleStoryTellerLatency(float timer, int layoutIndex, int itemIndex)
        {
            yield return new WaitForSeconds(timer);
            StartCoroutine(FinishStoryTeller(manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].replies[itemIndex].feedbackTimer, layoutIndex));

            GameObject timerObj = Instantiate(manager.chatMessageTimer, new Vector3(0, 0, 0), Quaternion.identity);
            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            timerObj.transform.SetParent(layout.messageParent, false);

            messageTimerObject = timerObj;
        }

        IEnumerator FinishStoryTeller(float timer, int layoutIndex)
        {
            yield return new WaitForSeconds(timer);

            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            MessagingChat.StoryTellerItem reply = manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].replies[manager.stItemIndex];

            // Always send the feedback text message first
            manager.CreateIndividualMessage(layout, reply.replyFeedback, reply.feedbackKey);

            // Then, if there's media in the feedback, send it as a separate message
            switch (reply.objectType)
            {
                case MessagingChat.ObjectType.AudioMessage:
                    manager.CreateIndividualAudioMessage(layout, reply.audioMessage, manager.GetTimeData());
                    break;
                case MessagingChat.ObjectType.ImageMessage:
                    manager.CreateIndividualImageMessage(layout, reply.imageMessage, "Image Feedback", "", manager.GetTimeData());
                    break;
            }

            if (!string.IsNullOrEmpty(reply.callAfter))
            {
                manager.CreateStoryTeller(manager.chatList[layoutIndex].chatTitle, reply.callAfter);
            }

            Destroy(messageTimerObject);
            Destroy(gameObject);
        }
    }
}