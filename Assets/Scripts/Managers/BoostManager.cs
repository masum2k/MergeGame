using UnityEngine;
using System.Collections.Generic;

public class BoostManager : MonoBehaviour
{
    public static BoostManager Instance { get; private set; }

    private class ActiveBoost
    {
        public BoostData data;
        public float remainingTime;
    }

    private List<ActiveBoost> _activeBoosts = new List<ActiveBoost>();

    public float CoinMultiplier { get; private set; } = 1.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (_activeBoosts.Count == 0) return;

        bool changed = false;
        for (int i = _activeBoosts.Count - 1; i >= 0; i--)
        {
            _activeBoosts[i].remainingTime -= Time.deltaTime;
            if (_activeBoosts[i].remainingTime <= 0)
            {
                _activeBoosts.RemoveAt(i);
                changed = true;
            }
        }

        if (changed) RecalculateMultipliers();
    }

    public void ActivateBoost(BoostData boost)
    {
        if (boost == null) return;

        _activeBoosts.Add(new ActiveBoost { data = boost, remainingTime = boost.durationSeconds });
        RecalculateMultipliers();
        Debug.Log($"Boost Activated: {boost.boostName}. Duration: {boost.durationSeconds}s");
    }

    private void RecalculateMultipliers()
    {
        float coinMult = 1.0f;
        foreach (var b in _activeBoosts)
        {
            if (b.data.type == BoostType.CoinMultiplier)
            {
                // Multipliers are usually additive or multiplicative? Let's go multiplicative for power.
                coinMult *= b.data.multiplier;
            }
        }
        CoinMultiplier = coinMult;
    }
}
