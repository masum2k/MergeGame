using UnityEngine;
using UnityEditor;

public class GameDevTools
{
    [MenuItem("Tools/Clear All Progress")]
    public static void ClearAllProgress()
    {
        if (EditorUtility.DisplayDialog("Clear Progress?", "Are you sure you want to delete all saved progress? This cannot be undone.", "Delete", "Cancel"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("<color=red><b>[GameDevTools]</b> ALL PROGRESS CLEARED!</color>");
            
            // If in play mode, suggest restarting manually for safety
            if (Application.isPlaying)
            {
                Debug.LogWarning("[GameDevTools] Progress cleared while in Play Mode. Please RESTART the game to see effects.");
            }
        }
    }
}
