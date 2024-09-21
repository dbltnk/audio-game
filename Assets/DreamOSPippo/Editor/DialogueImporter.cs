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

        // Parse Live Messages(StoryTeller)
        string liveMessagesText = dialogueText.Substring(messageHistoryEnd);

        Regex segmentRegex = new Regex(@"\[(?<id>(Pavel|Claire)_\d+)\](?<content>[\s\S]*?)(?=\[(Pavel|Claire)_\d+\]|\z)", RegexOptions.Multiline);

        var matches = segmentRegex.Matches(liveMessagesText);

        foreach (Match match in matches)
        {
            string segmentID = match.Groups["id"].Value;
            string segmentContent = match.Groups["content"].Value.Trim();

            MessagingChat.StoryTeller item = new MessagingChat.StoryTeller();
            item.itemID = segmentID; // Use the actual ID from the dialogue file

            // Process timing
            Match timingMatch = Regex.Match(segmentContent, @"\{latency:\s*(\d+(\.\d+)?),\s*timer:\s*(\d+(\.\d+)?)\}");
            if (timingMatch.Success)
            {
                item.messageLatency = float.Parse(timingMatch.Groups[1].Value);
                item.messageTimer = float.Parse(timingMatch.Groups[3].Value);
                segmentContent = segmentContent.Replace(timingMatch.Value, "").Trim();
            }

            // Process lines
            string[] lines = segmentContent.Split('\n');
            System.Text.StringBuilder contentBuilder = new System.Text.StringBuilder();
            int j = 0;
            while (j < lines.Length && !lines[j].StartsWith("1."))
            {
                string line = lines[j].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    j++;
                    continue;
                }
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
            while (j < lines.Length)
            {
                string currentLine = lines[j].Trim();

                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    // Skip empty lines
                    j++;
                    continue;
                }

                if (currentLine.StartsWith("1.") || currentLine.StartsWith("2.") || currentLine.StartsWith("3."))
                {
                    MessagingChat.StoryTellerItem reply = new MessagingChat.StoryTellerItem();
                    string[] parts = currentLine.Split('|');
                    if (parts.Length > 0) reply.replyBrief = parts[0].Substring(3).Trim();
                    if (parts.Length > 1) reply.replyContent = parts[1].Trim();
                    if (parts.Length > 2) reply.callAfter = parts[2].Trim();
                    reply.replyID = $"{item.itemID}_Reply_{item.replies.Count}";

                    j++; // Move to the next line to check for reply feedback

                    if (j < lines.Length)
                    {
                        string feedbackLine = lines[j].Trim();
                        if (!feedbackLine.StartsWith("1.") && !feedbackLine.StartsWith("2.") && !feedbackLine.StartsWith("3.") && !feedbackLine.StartsWith("["))
                        {
                            reply.replyFeedback = feedbackLine.Trim();

                            Match feedbackTimingMatch = Regex.Match(reply.replyFeedback, @"\{latency:\s*(\d+(\.\d+)?),\s*timer:\s*(\d+(\.\d+)?)\}");
                            if (feedbackTimingMatch.Success)
                            {
                                reply.feedbackLatency = float.Parse(feedbackTimingMatch.Groups[1].Value);
                                reply.feedbackTimer = float.Parse(feedbackTimingMatch.Groups[3].Value);
                                reply.replyFeedback = reply.replyFeedback.Replace(feedbackTimingMatch.Value, "").Trim();
                            }

                            j++; // Move to next line after processing feedback
                        }
                    }

                    item.replies.Add(reply);
                }
                else if (currentLine.StartsWith("["))
                {
                    // Reached a new dialogue segment, break out of the loop
                    break;
                }
                else
                {
                    // Skip unrecognized lines
                    j++;
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