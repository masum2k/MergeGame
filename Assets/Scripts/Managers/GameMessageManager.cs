using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameMessageEntry
{
    public string timestamp;
    public string message;
}

public class GameMessageManager : MonoBehaviour
{
    public static GameMessageManager Instance { get; private set; }

    public event Action OnMessagesChanged;

    public IReadOnlyList<GameMessageEntry> Messages => _messages;

    private readonly List<GameMessageEntry> _messages = new List<GameMessageEntry>();

    private const string MESSAGES_KEY = "GameMessagesData";
    private const int MAX_MESSAGES = 80;

    [Serializable]
    private class MessageWrapper
    {
        public List<GameMessageEntry> messages = new List<GameMessageEntry>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            EnsureDefaultMessages();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PushMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        GameMessageEntry entry = new GameMessageEntry
        {
            timestamp = DateTime.Now.ToString("MM-dd HH:mm"),
            message = text.Trim()
        };

        _messages.Insert(0, entry);

        if (_messages.Count > MAX_MESSAGES)
        {
            _messages.RemoveRange(MAX_MESSAGES, _messages.Count - MAX_MESSAGES);
        }

        Save();
        OnMessagesChanged?.Invoke();
    }

    private void EnsureDefaultMessages()
    {
        if (_messages.Count > 0)
            return;

        PushMessage("Hos geldin! Yeni market ve fabrika sistemi aktif.");
        PushMessage("Ipucu: Skill agacindan ekonomi dalini acmak coin akisini hizlandirir.");
    }

    private void Save()
    {
        MessageWrapper wrapper = new MessageWrapper { messages = _messages };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(MESSAGES_KEY, json);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        _messages.Clear();

        if (!PlayerPrefs.HasKey(MESSAGES_KEY))
            return;

        string json = PlayerPrefs.GetString(MESSAGES_KEY, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        MessageWrapper wrapper = JsonUtility.FromJson<MessageWrapper>(json);
        if (wrapper != null && wrapper.messages != null)
        {
            _messages.AddRange(wrapper.messages);
        }
    }
}
