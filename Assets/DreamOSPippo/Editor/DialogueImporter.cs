using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Michsky.DreamOS;

public class DialogueImporter : EditorWindow
{
    [MenuItem("Tools/Import Dialogue")]
    static void ShowWindow()
    {
        GetWindow<DialogueImporter>("Dialogue Importer");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Import/Update Dialogue (CSV Format)"))
        {
            string path = EditorUtility.OpenFilePanel("Select Dialogue File", "Assets/Dialogues-source", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                ImportOrUpdateDialogue(path);
            }
        }
    }

    void ImportOrUpdateDialogue(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
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

        ImportFromCSV(filePath, chatConversation);

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

    void ImportFromCSV(string filePath, MessagingChat chatConversation)
    {
        List<string[]> csvData = File.ReadAllLines(filePath)
            .Select(line => line.Split('|'))
            .ToList();

        // Skip header row
        for (int i = 1; i < csvData.Count; i++)
        {
            string[] row = csvData[i];
            string type = row[0];

            switch (type)
            {
                case "HISTORY":
                    ProcessHistoryMessage(chatConversation, row);
                    break;
                case "SEGMENT":
                    ProcessSegment(chatConversation, row);
                    break;
                case "REPLY":
                    ProcessReply(chatConversation, row);
                    break;
            }
        }
    }

    void ProcessHistoryMessage(MessagingChat chatConversation, string[] row)
    {
        MessagingChat.ChatMessage message = new MessagingChat.ChatMessage
        {
            messageContent = row[2],
            objectType = MessagingChat.ObjectType.Message,
            messageAuthor = row[3] == "Vince" ? MessagingChat.MessageAuthor.Self : MessagingChat.MessageAuthor.Individual,
            sentTime = row[4]
        };
        chatConversation.messageList.Add(message);
    }

    void ProcessSegment(MessagingChat chatConversation, string[] row)
    {
        MessagingChat.StoryTeller segment = new MessagingChat.StoryTeller
        {
            itemID = row[1],
            messageContent = row[2],
            messageAuthor = MessagingChat.MessageAuthor.Individual,
            messageLatency = float.TryParse(row[5], out float latency) ? latency : 0f,
            messageTimer = float.TryParse(row[6], out float timer) ? timer : 0f,
            replies = new List<MessagingChat.StoryTellerItem>()
        };
        chatConversation.storyTeller.Add(segment);
    }

    void ProcessReply(MessagingChat chatConversation, string[] row)
    {
        MessagingChat.StoryTeller segment = chatConversation.storyTeller.Find(s => s.itemID == row[1]);
        if (segment != null)
        {
            MessagingChat.StoryTellerItem reply = new MessagingChat.StoryTellerItem
            {
                replyID = row[7],
                replyBrief = row[8],
                replyContent = row[9],
                replyFeedback = row[10],
                callAfter = row[11]
            };
            segment.replies.Add(reply);
        }
    }
}