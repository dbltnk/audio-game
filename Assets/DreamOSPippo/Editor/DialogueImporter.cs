using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Michsky.DreamOS;
using System;

public class DialogueImporter : EditorWindow
{
    [MenuItem("Tools/Import Dialogue")]
    static void ShowWindow()
    {
        GetWindow<DialogueImporter>("Dialogue Importer");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Import/Update Dialogue"))
        {
            string path = EditorUtility.OpenFilePanel("Select Dialogue File", "Assets/Dialogues-source", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                ImportOrUpdateDialogue(path);
            }
        }
    }

    void ImportOrUpdateDialogue(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string dialogueText = File.ReadAllText(filePath);

        // Remove the comment section
        int commentEndIndex = dialogueText.IndexOf("--- START WRITING BELOW THIS LINE ---");
        if (commentEndIndex != -1)
        {
            dialogueText = dialogueText.Substring(commentEndIndex + "--- START WRITING BELOW THIS LINE ---".Length).Trim();
        }

        string assetPath = $"Assets/Dialogues-imported/{fileName}.asset";
        MessagingChat chatConversation = AssetDatabase.LoadAssetAtPath<MessagingChat>(assetPath);

        bool isNewAsset = chatConversation == null;

        if (isNewAsset)
        {
            chatConversation = ScriptableObject.CreateInstance<MessagingChat>();
        }

        chatConversation.saveConversation = false;
        chatConversation.useDynamicMessages = false;
        chatConversation.useStoryTeller = true;
        chatConversation.messageList = new List<MessagingChat.ChatMessage>();
        chatConversation.dynamicMessages = new List<MessagingChat.DynamicMessages>();
        chatConversation.storyTeller = new List<MessagingChat.StoryTeller>();

        // Parse Message History
        int messageHistoryStart = dialogueText.IndexOf("[MESSAGE HISTORY]", StringComparison.OrdinalIgnoreCase);
        int messageHistoryEnd = dialogueText.IndexOf("[LIVE MESSAGES]", StringComparison.OrdinalIgnoreCase);
        if (messageHistoryEnd == -1)
        {
            messageHistoryEnd = dialogueText.Length;
        }

        if (messageHistoryStart != -1)
        {
            string messageHistoryContent = dialogueText.Substring(messageHistoryStart, messageHistoryEnd - messageHistoryStart);
            string[] messageLines = messageHistoryContent.Split('\n');
            foreach (string line in messageLines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("[MESSAGE HISTORY]")) continue;

                Dictionary<string, string> messageParts = new Dictionary<string, string>();
                string[] parts = line.Split('|');
                foreach (string part in parts)
                {
                    string[] keyValue = part.Split(new[] { ':' }, 2);
                    if (keyValue.Length == 2)
                    {
                        messageParts[keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }

                if (messageParts.ContainsKey("content") && messageParts.ContainsKey("author") && messageParts.ContainsKey("time"))
                {
                    MessagingChat.ChatMessage message = new MessagingChat.ChatMessage
                    {
                        messageContent = messageParts["content"],
                        objectType = MessagingChat.ObjectType.Message,
                        messageAuthor = messageParts["author"] == "Vince" ? MessagingChat.MessageAuthor.Self : MessagingChat.MessageAuthor.Individual,
                        sentTime = messageParts["time"]
                    };
                    chatConversation.messageList.Add(message);
                }
            }
        }

        // Parse Live Messages (StoryTeller)
        string[] segments = Regex.Split(dialogueText.Substring(messageHistoryEnd), @"\[Pavel_\d+\]");

        for (int i = 1; i < segments.Length; i++)
        {
            string segment = segments[i].Trim();
            if (string.IsNullOrEmpty(segment)) continue;

            MessagingChat.StoryTeller item = new MessagingChat.StoryTeller();
            item.itemID = $"Pavel_{i - 1:D2}";

            Match timingMatch = Regex.Match(segment, @"\{latency:\s*(\d+(\.\d+)?),\s*timer:\s*(\d+(\.\d+)?)\}");
            if (timingMatch.Success)
            {
                item.messageLatency = float.Parse(timingMatch.Groups[1].Value);
                item.messageTimer = float.Parse(timingMatch.Groups[3].Value);
                segment = segment.Replace(timingMatch.Value, "").Trim();
            }

            string[] lines = segment.Split('\n');
            System.Text.StringBuilder contentBuilder = new System.Text.StringBuilder();
            int j = 0;
            while (j < lines.Length && !lines[j].StartsWith("1."))
            {
                string line = lines[j].Trim();
                if (line.StartsWith(">"))
                {
                    contentBuilder.AppendLine(line.Substring(1).Trim());
                }
                else if (line.StartsWith("[image:") || line.StartsWith("[audio:"))
                {
                    Debug.Log($"Media found for {item.itemID}: {line}");
                }
                else
                {
                    contentBuilder.AppendLine(line);
                }
                j++;
            }
            item.messageContent = contentBuilder.ToString().Trim();
            item.messageAuthor = MessagingChat.MessageAuthor.Individual;

            item.replies = new List<MessagingChat.StoryTellerItem>();
            for (; j < lines.Length; j++)
            {
                if (lines[j].StartsWith("1.") || lines[j].StartsWith("2.") || lines[j].StartsWith("3."))
                {
                    MessagingChat.StoryTellerItem reply = new MessagingChat.StoryTellerItem();
                    string[] parts = lines[j].Split('|');
                    if (parts.Length > 0) reply.replyBrief = parts[0].Substring(3).Trim();
                    if (parts.Length > 1) reply.replyContent = parts[1].Trim();
                    if (parts.Length > 2) reply.callAfter = parts[2].Trim();
                    reply.replyID = $"{item.itemID}_Reply_{item.replies.Count}";

                    if (j + 1 < lines.Length && lines[j + 1].TrimStart().StartsWith("<"))
                    {
                        reply.replyFeedback = lines[j + 1].TrimStart('<', ' ').Trim();

                        Match feedbackTimingMatch = Regex.Match(reply.replyFeedback, @"\{latency:\s*(\d+(\.\d+)?),\s*timer:\s*(\d+(\.\d+)?)\}");
                        if (feedbackTimingMatch.Success)
                        {
                            reply.feedbackLatency = float.Parse(feedbackTimingMatch.Groups[1].Value);
                            reply.feedbackTimer = float.Parse(feedbackTimingMatch.Groups[3].Value);
                            reply.replyFeedback = reply.replyFeedback.Replace(feedbackTimingMatch.Value, "").Trim();
                        }

                        j++;
                    }

                    item.replies.Add(reply);
                }
            }

            chatConversation.storyTeller.Add(item);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

        if (isNewAsset)
        {
            AssetDatabase.CreateAsset(chatConversation, assetPath);
            Debug.Log($"New dialogue asset created at {assetPath}");
        }
        else
        {
            EditorUtility.SetDirty(chatConversation);
            Debug.Log($"Existing dialogue asset updated at {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}