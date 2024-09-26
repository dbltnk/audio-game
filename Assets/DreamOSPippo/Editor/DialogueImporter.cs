using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Michsky.DreamOS;
using System.Text.RegularExpressions;

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
        }
        else
        {
            EditorUtility.SetDirty(chatConversation);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Dialogue asset {(isNewAsset ? "created" : "updated")} at {assetPath}");
    }

    void ImportFromCSV(string filePath, MessagingChat chatConversation)
    {
        List<string[]> csvData = ParseCSV(filePath);
        MessagingChat.StoryTeller currentSegment = null;

        // Skip header row
        for (int i = 1; i < csvData.Count; i++)
        {
            string[] row = csvData[i];
            string type = row[0];
            string messageType = row[12];

            switch (type)
            {
                case "HISTORY":
                    ProcessHistoryMessage(chatConversation, row, messageType);
                    break;
                case "SEGMENT":
                    ProcessSegment(chatConversation, row, messageType, ref currentSegment);
                    break;
                case "REPLY":
                    ProcessReply(currentSegment, row, messageType);
                    break;
            }
        }
    }

    void ProcessHistoryMessage(MessagingChat chatConversation, string[] row, string messageType)
    {
        MessagingChat.ChatMessage message = CreateChatMessage(row, messageType);
        chatConversation.messageList.Add(message);
    }

    void ProcessSegment(MessagingChat chatConversation, string[] row, string messageType, ref MessagingChat.StoryTeller currentSegment)
    {
        currentSegment = new MessagingChat.StoryTeller
        {
            itemID = row[1],
            messageContent = row[2],
            messageAuthor = row[3] == "Vince" ? MessagingChat.MessageAuthor.Self : MessagingChat.MessageAuthor.Individual,
            messageLatency = float.TryParse(row[5], out float latency) ? latency : 0f,
            messageTimer = float.TryParse(row[6], out float timer) ? timer : 0f,
            replies = new List<MessagingChat.StoryTellerItem>(),
            objectType = GetObjectTypeFromMessageType(messageType)
        };

        if (messageType == "IMAGE" || messageType == "AUDIO")
        {
            string assetPath = row[13];
            if (messageType == "IMAGE")
            {
                currentSegment.imageMessage = LoadSpriteFromPath(assetPath);
            }
            else if (messageType == "AUDIO")
            {
                currentSegment.audioMessage = LoadAudioClipFromPath(assetPath);
            }
        }

        chatConversation.storyTeller.Add(currentSegment);
    }

    void ProcessReply(MessagingChat.StoryTeller currentSegment, string[] row, string messageType)
    {
        if (currentSegment != null)
        {
            MessagingChat.StoryTellerItem reply = new MessagingChat.StoryTellerItem
            {
                replyID = row[7],
                replyBrief = row[8],
                replyContent = row[9],
                replyFeedback = row[10],
                callAfter = row[11],
                objectType = GetObjectTypeFromMessageType(messageType)
            };

            if (messageType == "IMAGE" || messageType == "AUDIO")
            {
                string assetPath = row[13];
                if (messageType == "IMAGE")
                {
                    reply.imageMessage = LoadSpriteFromPath(assetPath);
                }
                else if (messageType == "AUDIO")
                {
                    reply.audioMessage = LoadAudioClipFromPath(assetPath);
                }
            }

            currentSegment.replies.Add(reply);
        }
    }

    MessagingChat.ChatMessage CreateChatMessage(string[] row, string messageType)
    {
        MessagingChat.ChatMessage message = new MessagingChat.ChatMessage
        {
            messageContent = row[2],
            objectType = GetObjectTypeFromMessageType(messageType),
            messageAuthor = row[3] == "Vince" ? MessagingChat.MessageAuthor.Self : MessagingChat.MessageAuthor.Individual,
            sentTime = row[4]
        };

        if (messageType == "IMAGE" || messageType == "AUDIO")
        {
            string assetPath = row[13];
            if (messageType == "IMAGE")
            {
                message.imageMessage = LoadSpriteFromPath(assetPath);
            }
            else if (messageType == "AUDIO")
            {
                message.audioMessage = LoadAudioClipFromPath(assetPath);
            }
        }

        return message;
    }

    MessagingChat.ObjectType GetObjectTypeFromMessageType(string messageType)
    {
        switch (messageType)
        {
            case "TEXT":
                return MessagingChat.ObjectType.Message;
            case "IMAGE":
                return MessagingChat.ObjectType.ImageMessage;
            case "AUDIO":
                return MessagingChat.ObjectType.AudioMessage;
            case "DATE":
                return MessagingChat.ObjectType.Date;
            default:
                return MessagingChat.ObjectType.Message;
        }
    }

    Sprite LoadSpriteFromPath(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogError($"Failed to load sprite at path: {path}");
        }
        else
        {
            Debug.Log($"Successfully loaded sprite at path: {path}");
        }
        return sprite;
    }

    AudioClip LoadAudioClipFromPath(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogError($"Failed to load audio clip at path: {path}");
        }
        else
        {
            Debug.Log($"Successfully loaded audio clip at path: {path}");
        }
        return clip;
    }

    List<string[]> ParseCSV(string filePath)
    {
        List<string[]> parsedData = new List<string[]>();
        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.Trim());
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            fields.Add(currentField.Trim());
            parsedData.Add(fields.ToArray());
        }

        return parsedData;
    }
}