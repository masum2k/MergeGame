using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillTreeScreenPage : MonoBehaviour
{
    private const float VerticalLayoutXScale = 0.62f;
    private const float VerticalLayoutYScale = 0.55f;
    private const float VerticalLayoutYOffset = 20f;

    private class NodeView
    {
        public ResearchSkillDefinition skill;
        public Image bg;
        public Button button;
        public TextMeshProUGUI costText;
    }

    private readonly Dictionary<string, NodeView> _nodes = new Dictionary<string, NodeView>();

    private TextMeshProUGUI _researchPointsText;
    private TextMeshProUGUI _feedbackText;
    private RectTransform _boardRoot;

    private TextMeshProUGUI _detailTitleText;
    private TextMeshProUGUI _detailDescText;
    private TextMeshProUGUI _detailReqText;
    private TextMeshProUGUI _detailCostText;
    private Button _detailUnlockButton;

    private bool _built;
    private float _feedbackTimer;
    private string _selectedSkillId;

    private void Start()
    {
        BuildUI();
        SubscribeEvents();
        RefreshTree();

        if (ResearchManager.Instance != null)
        {
            List<ResearchSkillDefinition> skills = ResearchManager.Instance.GetAllSkills();
            if (skills.Count > 0)
            {
                SelectSkill(skills[0].id);
            }
        }
    }

    private void OnEnable()
    {
        if (_built)
        {
            RefreshTree();
            RefreshDetailPanel();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0f && _feedbackText != null)
            {
                _feedbackText.text = string.Empty;
            }
        }
    }

    private void BuildUI()
    {
        if (_built)
            return;

        RectTransform root = transform as RectTransform;

        Image bg = gameObject.GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.11f, 0.11f, 0.96f);
        bg.raycastTarget = true;

        GameObject panel = new GameObject("SkillTreePanel", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.16f, 0.16f, 0.96f);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.04f, 0.06f);
        panelRt.anchorMax = new Vector2(0.96f, 0.94f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "YETENEK AGACI";
        title.fontSize = 38;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.84f, 1f, 0.95f);

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -14f);
        titleRt.sizeDelta = new Vector2(0f, 48f);

        GameObject rpObj = new GameObject("ResearchPoints", typeof(RectTransform));
        rpObj.transform.SetParent(panel.transform, false);
        _researchPointsText = rpObj.AddComponent<TextMeshProUGUI>();
        _researchPointsText.fontSize = 24;
        _researchPointsText.fontStyle = FontStyles.Bold;
        _researchPointsText.alignment = TextAlignmentOptions.Center;
        _researchPointsText.color = new Color(0.72f, 1f, 0.8f);

        RectTransform rpRt = rpObj.GetComponent<RectTransform>();
        rpRt.anchorMin = new Vector2(0f, 1f);
        rpRt.anchorMax = new Vector2(1f, 1f);
        rpRt.pivot = new Vector2(0.5f, 1f);
        rpRt.anchoredPosition = new Vector2(0f, -56f);
        rpRt.sizeDelta = new Vector2(0f, 34f);

        GameObject boardObj = new GameObject("Board", typeof(RectTransform));
        boardObj.transform.SetParent(panel.transform, false);
        _boardRoot = boardObj.GetComponent<RectTransform>();
        Image boardBg = boardObj.AddComponent<Image>();
        boardBg.color = new Color(0.09f, 0.2f, 0.2f, 0.92f);
        boardObj.AddComponent<RectMask2D>();

        _boardRoot.anchorMin = new Vector2(0.02f, 0.1f);
        _boardRoot.anchorMax = new Vector2(0.72f, 0.84f);
        _boardRoot.offsetMin = Vector2.zero;
        _boardRoot.offsetMax = Vector2.zero;

        BuildDetailPanel(panel.transform);

        GameObject feedbackObj = new GameObject("Feedback", typeof(RectTransform));
        feedbackObj.transform.SetParent(panel.transform, false);
        _feedbackText = feedbackObj.AddComponent<TextMeshProUGUI>();
        _feedbackText.fontSize = 20;
        _feedbackText.alignment = TextAlignmentOptions.Center;
        _feedbackText.color = Color.white;

        RectTransform feedbackRt = feedbackObj.GetComponent<RectTransform>();
        feedbackRt.anchorMin = new Vector2(0f, 0f);
        feedbackRt.anchorMax = new Vector2(1f, 0f);
        feedbackRt.pivot = new Vector2(0.5f, 0f);
        feedbackRt.anchoredPosition = new Vector2(0f, 8f);
        feedbackRt.sizeDelta = new Vector2(0f, 34f);

        BuildTreeNodes();
        _built = true;
    }

    private void BuildDetailPanel(Transform parent)
    {
        GameObject detailObj = new GameObject("DetailPanel", typeof(RectTransform));
        detailObj.transform.SetParent(parent, false);
        Image detailBg = detailObj.AddComponent<Image>();
        detailBg.color = new Color(0.12f, 0.16f, 0.22f, 0.96f);

        RectTransform detailRt = detailObj.GetComponent<RectTransform>();
        detailRt.anchorMin = new Vector2(0.74f, 0.1f);
        detailRt.anchorMax = new Vector2(0.98f, 0.84f);
        detailRt.offsetMin = Vector2.zero;
        detailRt.offsetMax = Vector2.zero;

        GameObject dtTitleObj = new GameObject("DetailTitle", typeof(RectTransform));
        dtTitleObj.transform.SetParent(detailObj.transform, false);
        _detailTitleText = dtTitleObj.AddComponent<TextMeshProUGUI>();
        _detailTitleText.fontSize = 24;
        _detailTitleText.fontStyle = FontStyles.Bold;
        _detailTitleText.alignment = TextAlignmentOptions.Top;
        _detailTitleText.color = new Color(0.92f, 0.97f, 1f);

        RectTransform dttRt = dtTitleObj.GetComponent<RectTransform>();
        dttRt.anchorMin = new Vector2(0f, 1f);
        dttRt.anchorMax = new Vector2(1f, 1f);
        dttRt.pivot = new Vector2(0.5f, 1f);
        dttRt.anchoredPosition = new Vector2(0f, -12f);
        dttRt.sizeDelta = new Vector2(-16f, 42f);

        GameObject costObj = new GameObject("DetailCost", typeof(RectTransform));
        costObj.transform.SetParent(detailObj.transform, false);
        _detailCostText = costObj.AddComponent<TextMeshProUGUI>();
        _detailCostText.fontSize = 19;
        _detailCostText.fontStyle = FontStyles.Bold;
        _detailCostText.alignment = TextAlignmentOptions.Top;
        _detailCostText.color = new Color(1f, 0.88f, 0.46f);

        RectTransform dcRt = costObj.GetComponent<RectTransform>();
        dcRt.anchorMin = new Vector2(0f, 1f);
        dcRt.anchorMax = new Vector2(1f, 1f);
        dcRt.pivot = new Vector2(0.5f, 1f);
        dcRt.anchoredPosition = new Vector2(0f, -52f);
        dcRt.sizeDelta = new Vector2(-16f, 28f);

        GameObject descObj = new GameObject("DetailDescription", typeof(RectTransform));
        descObj.transform.SetParent(detailObj.transform, false);
        _detailDescText = descObj.AddComponent<TextMeshProUGUI>();
        _detailDescText.fontSize = 16;
        _detailDescText.alignment = TextAlignmentOptions.TopLeft;
        _detailDescText.color = new Color(0.83f, 0.91f, 0.98f);

        RectTransform ddRt = descObj.GetComponent<RectTransform>();
        ddRt.anchorMin = new Vector2(0f, 1f);
        ddRt.anchorMax = new Vector2(1f, 1f);
        ddRt.pivot = new Vector2(0.5f, 1f);
        ddRt.anchoredPosition = new Vector2(0f, -84f);
        ddRt.sizeDelta = new Vector2(-16f, 130f);

        GameObject reqObj = new GameObject("DetailReq", typeof(RectTransform));
        reqObj.transform.SetParent(detailObj.transform, false);
        _detailReqText = reqObj.AddComponent<TextMeshProUGUI>();
        _detailReqText.fontSize = 15;
        _detailReqText.alignment = TextAlignmentOptions.TopLeft;
        _detailReqText.color = new Color(0.75f, 0.84f, 0.94f);

        RectTransform drRt = reqObj.GetComponent<RectTransform>();
        drRt.anchorMin = new Vector2(0f, 1f);
        drRt.anchorMax = new Vector2(1f, 1f);
        drRt.pivot = new Vector2(0.5f, 1f);
        drRt.anchoredPosition = new Vector2(0f, -220f);
        drRt.sizeDelta = new Vector2(-16f, 160f);

        GameObject unlockObj = new GameObject("UnlockButton", typeof(RectTransform));
        unlockObj.transform.SetParent(detailObj.transform, false);
        Image ubBg = unlockObj.AddComponent<Image>();
        ubBg.color = new Color(0.3f, 0.72f, 0.98f, 1f);
        _detailUnlockButton = unlockObj.AddComponent<Button>();
        _detailUnlockButton.onClick.AddListener(OnUnlockSelectedClicked);

        RectTransform ubRt = unlockObj.GetComponent<RectTransform>();
        ubRt.anchorMin = new Vector2(0f, 0f);
        ubRt.anchorMax = new Vector2(1f, 0f);
        ubRt.pivot = new Vector2(0.5f, 0f);
        ubRt.offsetMin = new Vector2(12f, 10f);
        ubRt.offsetMax = new Vector2(-12f, 56f);

        GameObject ubTextObj = new GameObject("Text", typeof(RectTransform));
        ubTextObj.transform.SetParent(unlockObj.transform, false);
        TextMeshProUGUI ubText = ubTextObj.AddComponent<TextMeshProUGUI>();
        ubText.text = "Yetenegi Ac";
        ubText.fontSize = 20;
        ubText.fontStyle = FontStyles.Bold;
        ubText.alignment = TextAlignmentOptions.Center;
        ubText.color = new Color(0.08f, 0.1f, 0.14f);

        RectTransform ubtRt = ubTextObj.GetComponent<RectTransform>();
        ubtRt.anchorMin = Vector2.zero;
        ubtRt.anchorMax = Vector2.one;
        ubtRt.offsetMin = Vector2.zero;
        ubtRt.offsetMax = Vector2.zero;
    }

    private void BuildTreeNodes()
    {
        if (_boardRoot == null || ResearchManager.Instance == null)
            return;

        List<ResearchSkillDefinition> skills = ResearchManager.Instance.GetAllSkills();

        for (int i = 0; i < skills.Count; i++)
        {
            ResearchSkillDefinition skill = skills[i];
            NodeView node = CreateNode(skill);
            _nodes[skill.id] = node;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            ResearchSkillDefinition skill = skills[i];
            string[] prereqs = ResearchManager.Instance.GetPrerequisiteIds(skill);
            for (int j = 0; j < prereqs.Length; j++)
            {
                ResearchSkillDefinition prereq = ResearchManager.Instance.GetSkillById(prereqs[j]);
                if (prereq != null)
                {
                    CreateConnection(GetDisplayPosition(prereq), GetDisplayPosition(skill));
                }
            }
        }
    }

    private NodeView CreateNode(ResearchSkillDefinition skill)
    {
        GameObject nodeObj = new GameObject("Node_" + skill.id, typeof(RectTransform));
        nodeObj.transform.SetParent(_boardRoot, false);

        Image bg = nodeObj.AddComponent<Image>();
        Button button = nodeObj.AddComponent<Button>();

        RectTransform nodeRt = nodeObj.GetComponent<RectTransform>();
        nodeRt.anchorMin = new Vector2(0.5f, 0.5f);
        nodeRt.anchorMax = new Vector2(0.5f, 0.5f);
        nodeRt.pivot = new Vector2(0.5f, 0.5f);
        nodeRt.anchoredPosition = GetDisplayPosition(skill);
        nodeRt.sizeDelta = new Vector2(98f, 62f);

        GameObject costObj = new GameObject("Cost", typeof(RectTransform));
        costObj.transform.SetParent(nodeObj.transform, false);
        TextMeshProUGUI cost = costObj.AddComponent<TextMeshProUGUI>();
        cost.fontSize = 17;
        cost.fontStyle = FontStyles.Bold;
        cost.alignment = TextAlignmentOptions.Center;

        RectTransform costRt = costObj.GetComponent<RectTransform>();
        costRt.anchorMin = Vector2.zero;
        costRt.anchorMax = Vector2.one;
        costRt.offsetMin = Vector2.zero;
        costRt.offsetMax = Vector2.zero;

        string id = skill.id;
        button.onClick.AddListener(() => SelectSkill(id));

        return new NodeView
        {
            skill = skill,
            bg = bg,
            button = button,
            costText = cost
        };
    }

    private void CreateConnection(Vector2 from, Vector2 to)
    {
        GameObject lineObj = new GameObject("Connection", typeof(RectTransform));
        lineObj.transform.SetParent(_boardRoot, false);
        Image line = lineObj.AddComponent<Image>();
        line.color = new Color(0.55f, 0.8f, 0.78f, 0.4f);

        RectTransform lineRt = lineObj.GetComponent<RectTransform>();
        Vector2 mid = (from + to) * 0.5f;
        Vector2 diff = to - from;

        lineRt.anchorMin = new Vector2(0.5f, 0.5f);
        lineRt.anchorMax = new Vector2(0.5f, 0.5f);
        lineRt.pivot = new Vector2(0.5f, 0.5f);
        lineRt.anchoredPosition = mid;
        lineRt.sizeDelta = new Vector2(diff.magnitude, 3f);
        lineRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);

        lineObj.transform.SetAsFirstSibling();
    }

    private Vector2 GetDisplayPosition(ResearchSkillDefinition skill)
    {
        if (skill == null)
            return Vector2.zero;

        // Convert horizontal catalog coordinates into a vertical progression.
        float x = skill.layoutPosition.y * VerticalLayoutXScale;
        float y = skill.layoutPosition.x * VerticalLayoutYScale + VerticalLayoutYOffset;
        return new Vector2(x, y);
    }

    private void RefreshTree()
    {
        if (ResearchManager.Instance == null)
            return;

        if (_researchPointsText != null)
        {
            _researchPointsText.text = "Arastirma Puani: " + ResearchManager.Instance.ResearchPoints;
        }

        foreach (KeyValuePair<string, NodeView> kvp in _nodes)
        {
            NodeView node = kvp.Value;
            if (node == null || node.skill == null)
                continue;

            bool unlocked = ResearchManager.Instance.IsUnlocked(node.skill.id);
            bool canUnlock = ResearchManager.Instance.CanUnlock(node.skill.id, out _);
            bool selected = _selectedSkillId == node.skill.id;

            Color baseColor = ResearchManager.Instance.GetCategoryColor(node.skill.categoryIndex);

            if (unlocked)
            {
                node.bg.color = Color.Lerp(baseColor, Color.white, 0.42f);
                node.costText.text = "OK";
                node.costText.color = new Color(0.08f, 0.2f, 0.1f);
            }
            else if (canUnlock)
            {
                node.bg.color = baseColor;
                node.costText.text = node.skill.cost + " RP";
                node.costText.color = new Color(0.09f, 0.1f, 0.12f);
            }
            else
            {
                node.bg.color = Color.Lerp(baseColor, new Color(0.18f, 0.2f, 0.24f, 1f), 0.62f);
                node.costText.text = node.skill.cost + " RP";
                node.costText.color = new Color(0.78f, 0.84f, 0.9f);
            }

            RectTransform rt = node.bg.rectTransform;
            rt.localScale = selected ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
        }
    }

    private void SelectSkill(string skillId)
    {
        _selectedSkillId = skillId;
        RefreshTree();
        RefreshDetailPanel();
    }

    private void RefreshDetailPanel()
    {
        if (ResearchManager.Instance == null)
            return;

        ResearchSkillDefinition skill = ResearchManager.Instance.GetSkillById(_selectedSkillId);
        if (skill == null)
        {
            _detailTitleText.text = "Yetenek sec";
            _detailDescText.text = "Sol taraftan bir dugume tikla.";
            _detailReqText.text = string.Empty;
            _detailCostText.text = string.Empty;
            _detailUnlockButton.interactable = false;
            return;
        }

        _detailTitleText.text = skill.title;
        _detailDescText.text = skill.description;
        _detailCostText.text = "Maliyet: " + skill.cost + " RP";

        string reqText = "Onkosullar:\n";
        string[] prereqs = ResearchManager.Instance.GetPrerequisiteIds(skill);
        if (prereqs.Length == 0)
        {
            reqText += "- Yok";
        }
        else
        {
            for (int i = 0; i < prereqs.Length; i++)
            {
                ResearchSkillDefinition prereq = ResearchManager.Instance.GetSkillById(prereqs[i]);
                bool ok = ResearchManager.Instance.IsUnlocked(prereqs[i]);
                reqText += "- [" + (ok ? "OK" : "X") + "] " + (prereq != null ? prereq.title : prereqs[i]) + "\n";
            }
        }

        if (ResearchManager.Instance.CanUnlock(skill.id, out string reason))
        {
            reqText += "\nDurum: Acilabilir";
            _detailUnlockButton.interactable = true;
        }
        else
        {
            reqText += "\nDurum: " + reason;
            _detailUnlockButton.interactable = !ResearchManager.Instance.IsUnlocked(skill.id);
        }

        if (ResearchManager.Instance.IsUnlocked(skill.id))
        {
            _detailUnlockButton.interactable = false;
            reqText += "\nDurum: Acildi";
        }

        _detailReqText.text = reqText;
    }

    private void OnUnlockSelectedClicked()
    {
        if (ResearchManager.Instance == null || string.IsNullOrEmpty(_selectedSkillId))
            return;

        bool success = ResearchManager.Instance.UnlockSkill(_selectedSkillId, out string message);
        ShowFeedback(message, success ? new Color(0.52f, 1f, 0.68f) : new Color(1f, 0.54f, 0.54f));
        RefreshTree();
        RefreshDetailPanel();
    }

    private void ShowFeedback(string message, Color color)
    {
        if (_feedbackText == null)
            return;

        _feedbackText.text = message;
        _feedbackText.color = color;
        _feedbackTimer = 2.4f;
    }

    private void SubscribeEvents()
    {
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnResearchPointsChanged += HandleResearchPointsChanged;
            ResearchManager.Instance.OnSkillStateChanged += HandleSkillStateChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnResearchPointsChanged -= HandleResearchPointsChanged;
            ResearchManager.Instance.OnSkillStateChanged -= HandleSkillStateChanged;
        }
    }

    private void HandleResearchPointsChanged(int _)
    {
        RefreshTree();
        RefreshDetailPanel();
    }

    private void HandleSkillStateChanged()
    {
        RefreshTree();
        RefreshDetailPanel();
    }
}
