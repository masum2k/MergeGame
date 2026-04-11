using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ClickerManager — Manages energy-based clicking for coin generation.
/// The visual button is created by UIManager (bottom-center round yellow button).
/// This script handles only the logic: energy tracking, regen, and click rewards.
/// </summary>
public class ClickerManager : MonoBehaviour
{
    [Header("Settings")]
    public int clickReward = 1;
    public int maxEnergy = 10;
    
    // State
    private int currentEnergy;
    private float timer = 0f;

    // Event
    public static event System.Action<int, int> OnEnergyChanged;

    private void Start()
    {
        currentEnergy = maxEnergy;
        // Notify any listeners (UIManager will update button text)
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
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
            }
        }
        else
        {
            timer = 0f;
        }
    }

    /// <summary>
    /// This method is called by UIManager's clicker button via onClick.
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
        }
    }
}
