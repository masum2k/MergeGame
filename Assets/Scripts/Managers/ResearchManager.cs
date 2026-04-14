using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public enum ResearchSkillType
{
    CropIncomePercent,
    GlobalIncomePercent,
    ClickRewardFlat,
    ChestCostDiscountPercent,
    FactoryRPPercent,
    FactoryGemPercent
}

[Serializable]
public class ResearchSkillDefinition
{
    public string id;
    public string title;
    public string description;
    public int cost;
    public ResearchSkillType type;
    public string targetCropName;
    public float value;

    // Backward compatibility for single prerequisite setups.
    public string prerequisiteId;
    public string[] prerequisiteIds;

    public Vector2 layoutPosition;
    public int categoryIndex;
}

public class ResearchManager : MonoBehaviour
{
    public static ResearchManager Instance { get; private set; }

    public int ResearchPoints { get; private set; }
    public int TotalResearchSpent { get; private set; }

    public event Action<int> OnResearchPointsChanged;
    public event Action OnSkillStateChanged;

    private readonly List<ResearchSkillDefinition> _skills = new List<ResearchSkillDefinition>();
    private readonly HashSet<string> _unlockedSkillIds = new HashSet<string>();

    private const string RESEARCH_POINTS_KEY = "ResearchPoints";
    private const string RESEARCH_SPENT_KEY = "ResearchSpent";
    private const string UNLOCKED_SKILLS_KEY = "UnlockedResearchSkills";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSkillCatalog();
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public List<ResearchSkillDefinition> GetAllSkills()
    {
        return _skills;
    }

    public ResearchSkillDefinition GetSkillById(string skillId)
    {
        return _skills.Find(s => s.id == skillId);
    }

    public bool IsUnlocked(string skillId)
    {
        return _unlockedSkillIds.Contains(skillId);
    }

    public bool CanUnlock(string skillId, out string reason)
    {
        ResearchSkillDefinition skill = GetSkillById(skillId);
        if (skill == null)
        {
            reason = "Yetenek bulunamadi.";
            return false;
        }

        if (IsUnlocked(skillId))
        {
            reason = "Bu yetenek zaten acildi.";
            return false;
        }

        string[] prereqIds = GetPrerequisiteIds(skill);
        for (int i = 0; i < prereqIds.Length; i++)
        {
            string prereqId = prereqIds[i];
            if (string.IsNullOrEmpty(prereqId))
                continue;

            if (!IsUnlocked(prereqId))
            {
                ResearchSkillDefinition prereq = GetSkillById(prereqId);
                reason = "Onkosul eksik: " + (prereq != null ? prereq.title : prereqId);
                return false;
            }
        }

        if (ResearchPoints < skill.cost)
        {
            reason = "Yetersiz arastirma puani.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool UnlockSkill(string skillId, out string message)
    {
        if (!CanUnlock(skillId, out message))
            return false;

        ResearchSkillDefinition skill = GetSkillById(skillId);
        ResearchPoints -= skill.cost;
        TotalResearchSpent += skill.cost;
        _unlockedSkillIds.Add(skillId);

        Save();
        OnResearchPointsChanged?.Invoke(ResearchPoints);
        OnSkillStateChanged?.Invoke();

        message = skill.title + " acildi!";
        GameMessageManager.Instance?.PushMessage("Yeni yetenek acildi: " + skill.title);
        return true;
    }

    public void AddResearchPoints(int amount)
    {
        if (amount <= 0)
            return;

        ResearchPoints += amount;
        Save();
        OnResearchPointsChanged?.Invoke(ResearchPoints);
    }

    public float GetCropIncomeMultiplier(CropData crop)
    {
        if (crop == null)
            return 1f;

        float bonusPercent = 0f;

        for (int i = 0; i < _skills.Count; i++)
        {
            ResearchSkillDefinition skill = _skills[i];
            if (!IsUnlocked(skill.id))
                continue;

            if (skill.type == ResearchSkillType.GlobalIncomePercent)
            {
                bonusPercent += skill.value;
            }
            else if (skill.type == ResearchSkillType.CropIncomePercent &&
                     !string.IsNullOrEmpty(skill.targetCropName) &&
                     string.Equals(NormalizeForCompare(skill.targetCropName), NormalizeForCompare(crop.itemName), StringComparison.OrdinalIgnoreCase))
            {
                bonusPercent += skill.value;
            }
        }

        return 1f + Mathf.Max(0f, bonusPercent);
    }

    public int GetClickRewardBonus()
    {
        int bonus = 0;

        for (int i = 0; i < _skills.Count; i++)
        {
            ResearchSkillDefinition skill = _skills[i];
            if (!IsUnlocked(skill.id))
                continue;

            if (skill.type == ResearchSkillType.ClickRewardFlat)
            {
                bonus += Mathf.RoundToInt(skill.value);
            }
        }

        return bonus;
    }

    public float GetFactoryRpMultiplier()
    {
        float bonus = 0f;

        for (int i = 0; i < _skills.Count; i++)
        {
            ResearchSkillDefinition skill = _skills[i];
            if (!IsUnlocked(skill.id))
                continue;

            if (skill.type == ResearchSkillType.FactoryRPPercent)
            {
                bonus += skill.value;
            }
        }

        return 1f + Mathf.Max(0f, bonus);
    }

    public float GetFactoryGemMultiplier()
    {
        float bonus = 0f;

        for (int i = 0; i < _skills.Count; i++)
        {
            ResearchSkillDefinition skill = _skills[i];
            if (!IsUnlocked(skill.id))
                continue;

            if (skill.type == ResearchSkillType.FactoryGemPercent)
            {
                bonus += skill.value;
            }
        }

        return 1f + Mathf.Max(0f, bonus);
    }

    public float GetChestCostMultiplier()
    {
        float discount = 0f;

        for (int i = 0; i < _skills.Count; i++)
        {
            ResearchSkillDefinition skill = _skills[i];
            if (!IsUnlocked(skill.id))
                continue;

            if (skill.type == ResearchSkillType.ChestCostDiscountPercent)
            {
                discount += skill.value;
            }
        }

        return Mathf.Clamp(1f - discount, 0.35f, 1f);
    }

    public string[] GetPrerequisiteIds(ResearchSkillDefinition skill)
    {
        if (skill == null)
            return Array.Empty<string>();

        if (skill.prerequisiteIds != null && skill.prerequisiteIds.Length > 0)
            return skill.prerequisiteIds;

        if (!string.IsNullOrEmpty(skill.prerequisiteId))
            return new[] { skill.prerequisiteId };

        return Array.Empty<string>();
    }

    public Color GetCategoryColor(int categoryIndex)
    {
        switch (categoryIndex)
        {
            case 0: return new Color(0.96f, 0.8f, 0.24f, 1f); // Economy
            case 1: return new Color(0.38f, 0.86f, 0.42f, 1f); // Agriculture
            case 2: return new Color(1f, 0.56f, 0.24f, 1f); // Industry
            case 3: return new Color(0.34f, 0.86f, 1f, 1f); // Science
            default: return new Color(0.72f, 0.78f, 0.84f, 1f);
        }
    }

    private void BuildSkillCatalog()
    {
        _skills.Clear();

        // Economy (yellow)
        AddSkill("e_root", "Pazar Analizi", "Tum gelir +%8.", 6, ResearchSkillType.GlobalIncomePercent, 0.08f, 0, -520f, 220f);
        AddSkill("e_click1", "Mikro Satis", "Tiklama odulu +1.", 8, ResearchSkillType.ClickRewardFlat, 1f, 0, -420f, 120f, prereqs: new[] { "e_root" });
        AddSkill("e_market1", "Toptanci Baglanti", "Sandik maliyeti -%5.", 9, ResearchSkillType.ChestCostDiscountPercent, 0.05f, 0, -620f, 120f, prereqs: new[] { "e_root" });
        AddSkill("e_global2", "Gelir Optimizasyonu", "Tum gelir +%12.", 12, ResearchSkillType.GlobalIncomePercent, 0.12f, 0, -520f, 20f, prereqs: new[] { "e_click1", "e_market1" });
        AddSkill("e_click2", "Hizli Satis", "Tiklama odulu +2.", 14, ResearchSkillType.ClickRewardFlat, 2f, 0, -420f, -80f, prereqs: new[] { "e_global2" });
        AddSkill("e_market2", "Lojistik Agi", "Sandik maliyeti -%7.", 14, ResearchSkillType.ChestCostDiscountPercent, 0.07f, 0, -620f, -80f, prereqs: new[] { "e_global2" });
        AddSkill("e_global3", "Makro Ekonomi", "Tum gelir +%20.", 20, ResearchSkillType.GlobalIncomePercent, 0.20f, 0, -520f, -180f, prereqs: new[] { "e_click2", "e_market2" });

        // Agriculture (green)
        AddSkill("a_carrot", "Havuc Verimi", "Havuc gelirleri +%20.", 5, ResearchSkillType.CropIncomePercent, 0.20f, 1, -220f, 220f, "Havuc");
        AddSkill("a_tomato", "Domates Verimi", "Domates gelirleri +%16.", 6, ResearchSkillType.CropIncomePercent, 0.16f, 1, -120f, 120f, "Domates", new[] { "a_carrot" });
        AddSkill("a_corn", "Misir Verimi", "Misir gelirleri +%16.", 6, ResearchSkillType.CropIncomePercent, 0.16f, 1, -320f, 120f, "Misir", new[] { "a_carrot" });
        AddSkill("a_mix1", "Toprak Harmani", "Tum gelir +%6.", 10, ResearchSkillType.GlobalIncomePercent, 0.06f, 1, -220f, 20f, prereqs: new[] { "a_tomato", "a_corn" });
        AddSkill("a_pumpkin", "Balkabagi Verimi", "Balkabagi gelirleri +%24.", 12, ResearchSkillType.CropIncomePercent, 0.24f, 1, -120f, -80f, "Balkabagi", new[] { "a_mix1" });
        AddSkill("a_watermelon", "Karpuz Verimi", "Karpuz gelirleri +%26.", 12, ResearchSkillType.CropIncomePercent, 0.26f, 1, -320f, -80f, "Karpuz", new[] { "a_mix1" });
        AddSkill("a_mix2", "Genetik Ciftlik", "Tum gelir +%10.", 18, ResearchSkillType.GlobalIncomePercent, 0.10f, 1, -220f, -180f, prereqs: new[] { "a_pumpkin", "a_watermelon" });

        // Industry (orange)
        AddSkill("i_root", "Hat Planlama", "Fabrika RP odulu +%20.", 7, ResearchSkillType.FactoryRPPercent, 0.20f, 2, 80f, 220f);
        AddSkill("i_gem1", "Saflastirma", "Fabrika Gem odulu +%20.", 8, ResearchSkillType.FactoryGemPercent, 0.20f, 2, 180f, 120f, prereqs: new[] { "i_root" });
        AddSkill("i_rp2", "Makine Dengesi", "Fabrika RP odulu +%25.", 10, ResearchSkillType.FactoryRPPercent, 0.25f, 2, -20f, 120f, prereqs: new[] { "i_root" });
        AddSkill("i_eff1", "Verim Mimarisi", "Sandik maliyeti -%4.", 11, ResearchSkillType.ChestCostDiscountPercent, 0.04f, 2, 80f, 20f, prereqs: new[] { "i_gem1", "i_rp2" });
        AddSkill("i_gem2", "Kristal Isleme", "Fabrika Gem odulu +%30.", 15, ResearchSkillType.FactoryGemPercent, 0.30f, 2, 180f, -80f, prereqs: new[] { "i_eff1" });
        AddSkill("i_rp3", "Optimum Hat", "Fabrika RP odulu +%35.", 15, ResearchSkillType.FactoryRPPercent, 0.35f, 2, -20f, -80f, prereqs: new[] { "i_eff1" });
        AddSkill("i_eff2", "Mega Fabrika", "Tum gelir +%8.", 19, ResearchSkillType.GlobalIncomePercent, 0.08f, 2, 80f, -180f, prereqs: new[] { "i_gem2", "i_rp3" });

        // Science (cyan)
        AddSkill("s_root", "Veri Bilimi", "Tum gelir +%5.", 6, ResearchSkillType.GlobalIncomePercent, 0.05f, 3, 380f, 220f);
        AddSkill("s_click1", "Analitik Tiklama", "Tiklama odulu +1.", 7, ResearchSkillType.ClickRewardFlat, 1f, 3, 480f, 120f, prereqs: new[] { "s_root" });
        AddSkill("s_factory1", "Deneysel Hat", "Fabrika RP odulu +%15.", 9, ResearchSkillType.FactoryRPPercent, 0.15f, 3, 280f, 120f, prereqs: new[] { "s_root" });
        AddSkill("s_cross1", "Model Birlesimi", "Tum gelir +%9.", 12, ResearchSkillType.GlobalIncomePercent, 0.09f, 3, 380f, 20f, prereqs: new[] { "s_click1", "s_factory1" });
        AddSkill("s_market", "Talep Simulasyonu", "Sandik maliyeti -%5.", 13, ResearchSkillType.ChestCostDiscountPercent, 0.05f, 3, 480f, -80f, prereqs: new[] { "s_cross1" });
        AddSkill("s_factory2", "Nano Islem", "Fabrika Gem odulu +%18.", 13, ResearchSkillType.FactoryGemPercent, 0.18f, 3, 280f, -80f, prereqs: new[] { "s_cross1" });
        AddSkill("s_cross2", "Otonom Sistem", "Tum gelir +%14.", 20, ResearchSkillType.GlobalIncomePercent, 0.14f, 3, 380f, -180f, prereqs: new[] { "s_market", "s_factory2" });
    }

    private void AddSkill(
        string id,
        string title,
        string description,
        int cost,
        ResearchSkillType type,
        float value,
        int category,
        float x,
        float y,
        string targetCrop = "",
        string[] prereqs = null)
    {
        ResearchSkillDefinition skill = new ResearchSkillDefinition
        {
            id = id,
            title = title,
            description = description,
            cost = cost,
            type = type,
            value = value,
            categoryIndex = category,
            layoutPosition = new Vector2(x, y),
            targetCropName = targetCrop,
            prerequisiteIds = prereqs
        };

        if (prereqs != null && prereqs.Length > 0)
        {
            skill.prerequisiteId = prereqs[0];
        }

        _skills.Add(skill);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(RESEARCH_POINTS_KEY, ResearchPoints);
        PlayerPrefs.SetInt(RESEARCH_SPENT_KEY, TotalResearchSpent);
        PlayerPrefs.SetString(UNLOCKED_SKILLS_KEY, string.Join("|", _unlockedSkillIds));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        ResearchPoints = PlayerPrefs.GetInt(RESEARCH_POINTS_KEY, 0);
        TotalResearchSpent = PlayerPrefs.GetInt(RESEARCH_SPENT_KEY, 0);
        _unlockedSkillIds.Clear();

        string unlockedRaw = PlayerPrefs.GetString(UNLOCKED_SKILLS_KEY, string.Empty);
        if (!string.IsNullOrEmpty(unlockedRaw))
        {
            string[] parts = unlockedRaw.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(parts[i]))
                {
                    _unlockedSkillIds.Add(parts[i]);
                }
            }
        }

        // Drop stale unlock ids if catalog changed.
        HashSet<string> validIds = new HashSet<string>();
        for (int i = 0; i < _skills.Count; i++)
        {
            validIds.Add(_skills[i].id);
        }

        List<string> staleIds = new List<string>();
        foreach (string id in _unlockedSkillIds)
        {
            if (!validIds.Contains(id))
            {
                staleIds.Add(id);
            }
        }

        for (int i = 0; i < staleIds.Count; i++)
        {
            _unlockedSkillIds.Remove(staleIds[i]);
        }

        OnResearchPointsChanged?.Invoke(ResearchPoints);
        OnSkillStateChanged?.Invoke();
    }

    private string NormalizeForCompare(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string decomposed = value.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder(decomposed.Length);

        for (int i = 0; i < decomposed.Length; i++)
        {
            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(decomposed[i]);
            if (cat != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(decomposed[i]);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
