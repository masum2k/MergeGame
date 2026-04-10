using UnityEngine;
using UnityEngine.UI;

public class ClickerManager : MonoBehaviour
{
    [Header("Settings")]
    public int clickReward = 1;
    public int maxEnergy = 10;
    
    // State
    private int currentEnergy;
    private float timer = 0f;
    
    // UI binding elements
    private Button myButton;
    private TMPro.TextMeshProUGUI myButtonText;

    // Event
    public static event System.Action<int, int> OnEnergyChanged;

    private void Start()
    {
        currentEnergy = maxEnergy;
        // Zero-Touch: Find the "Tıkla Kazan" button dynamically
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            var pText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (pText != null && pText.text.Contains("Tıkla"))
            {
                myButton = btn;
                myButtonText = pText;
                break;
            }
        }
        
        // Notify any listeners
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        UpdateButtonState();
        UpdateButtonText();
    }
    
    private void Update()
    {
        if (currentEnergy < maxEnergy)
        {
            timer += Time.deltaTime;
            if (timer >= 10f)
            {
                currentEnergy++;
                timer -= 10f;
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
                UpdateButtonState();
                UpdateButtonText();
            }
        }
        else
        {
            timer = 0f;
        }
    }

    /// <summary>
    /// This method is designed to be called directly from a UI Button's "On Click ()" event via the Unity Inspector.
    /// </summary>
    public void OnClickCoinButton()
    {
        if (currentEnergy > 0)
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoin(clickReward);
            }
            
            currentEnergy--;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            UpdateButtonState();
            UpdateButtonText();
        }
    }
    
    private void UpdateButtonState()
    {
        if (myButton != null)
        {
            myButton.interactable = currentEnergy > 0;
        }
    }

    private void UpdateButtonText()
    {
        if (myButtonText != null)
        {
            if (currentEnergy >= maxEnergy)
            {
                myButtonText.text = "MAX";
            }
            else
            {
                myButtonText.text = currentEnergy.ToString();
            }
        }
    }
}
