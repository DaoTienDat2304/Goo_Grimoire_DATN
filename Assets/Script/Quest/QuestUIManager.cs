using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class QuestUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject questItemPrefab;  // Prefab QuestItem
    public Transform contentParent;     // Content của ScrollView

    [Header("Quest Colors")]
    public Color lockedColor = Color.gray;
    public Color availableColor = Color.white;
    public Color inProgressColor = Color.yellow;
    public Color completedColor = Color.green;
    public Color rewardedColor = Color.blue;

    private Dictionary<int, GameObject> questItems = new Dictionary<int, GameObject>();
    private Dictionary<int, Quest> questData = new Dictionary<int, Quest>();

    // Khi thêm quest mới vào UI
    public void AddQuest(Quest quest)
    {
        if (questItems.ContainsKey(quest.questID)) return;

        GameObject newItem = Instantiate(questItemPrefab, contentParent);
        questData[quest.questID] = quest;

        var general = GameObject.Find("generalQuest");
        if (general != null)
        {
            newItem.transform.SetParent(general.transform, false);
        }
        // Daily luôn đẩy lên đầu danh sách; nhiệm vụ thường xuống cuối.
        if (quest is DailyQuest) newItem.transform.SetAsFirstSibling();
        else newItem.transform.SetAsLastSibling();

        // Tìm các component UI
        TMP_Text[] texts = newItem.GetComponentsInChildren<TMP_Text>();
        Text progression = newItem.GetComponentInChildren<Text>();
        Image questBackground = newItem.GetComponent<Image>();
        Slider progressBar = newItem.GetComponentInChildren<Slider>();
        Button claimButton = newItem.GetComponentInChildren<Button>();
        Image rewardIcon = newItem.transform.Find("RewardImage").GetComponent<Image>();

        // Thiết lập thông tin cơ bản
        if (texts.Length >= 3)
        {
            texts[0].text = quest.questName;        // Quest Name
            texts[1].text = quest.description;
            switch (quest.state)
            {
                case Quest.QuestState.Locked:
                    texts[2].text = "🔒 Locked";
                    break;
                case Quest.QuestState.Available:
                    texts[2].text = "✅ Available";
                    break;
                case Quest.QuestState.InProgress:
                    texts[2].text = "⏳ In Progress";
                    break;
                case Quest.QuestState.Completed:
                    texts[2].text = "🎉 Completed";
                    break;
                case Quest.QuestState.Rewarded:
                    texts[2].text = "💰 Rewarded";
                    break;
                default:
                    texts[2].text = "Unknown";
                    break;
            }

            // Hiển thị reward nếu có - sửa index từ 3 thành 4
            if (texts.Length >= 5 && quest.reward != null)
            {
                Debug.Log($"Reward: {quest.reward.amount} {quest.reward.rewardType}");
                texts[3].text = $"Reward: {quest.reward.amount} {quest.reward.rewardType}";
            }
            else
            {
                // Debug: Kiểm tra tại sao reward không hiển thị
                Debug.Log($"Quest '{quest.questName}' - texts.Length: {texts.Length}, reward: {(quest.reward != null ? "exists" : "null")}");
                if (texts.Length >= 5)
                {
                    texts[3].text = "No Reward Set";
                }
            }
            if (rewardIcon == questBackground) Debug.Log("rewardIcon is questBackground");
            rewardIcon.sprite = quest.rewardIcon;
            UpdateQuestDisplay(quest, texts, progression, questBackground, progressBar, claimButton);
        }

        QuestUIEffects.SetDimmed(newItem, quest.state == Quest.QuestState.Rewarded);
        questItems.Add(quest.questID, newItem);
    }

    // Cập nhật trạng thái quest trên UI
    public void UpdateQuestState(Quest quest)
    {
        if (!questItems.ContainsKey(quest.questID)) return;

        GameObject questItem = questItems[quest.questID];
        TMP_Text[] texts = questItem.GetComponentsInChildren<TMP_Text>();
        Text progresion = questItem.GetComponentInChildren<Text>();
        Image questBackground = questItem.GetComponent<Image>();
        Slider progressBar = questItem.GetComponentInChildren<Slider>();
        Button claimButton = questItem.GetComponentInChildren<Button>();

        UpdateQuestDisplay(quest, texts, progresion, questBackground, progressBar, claimButton);
        QuestUIEffects.SetDimmed(questItem, quest.state == Quest.QuestState.Rewarded);
    }

    private void UpdateQuestDisplay(Quest quest, TMP_Text[] texts, Text progresion, Image questBackground, Slider progressBar, Button claimButton)
    {
        // Cập nhật màu nền
        if (questBackground != null)
        {

        }

        // Cập nhật progress bar
        if (progressBar != null)
        {
            float progress = GetQuestProgress(quest);
            progressBar.value = progress;
            
            // Hiển thị progress text nếu có - sửa để tránh infinite recursion
            if (texts.Length >= 4)
            {
                progresion.text = quest.GetProgressText();
            }
        }

        // Cập nhật nút claim reward
        if (claimButton != null && quest.state == Quest.QuestState.Completed)
        {
            claimButton.gameObject.SetActive(true);
            if (quest.state == Quest.QuestState.Completed)
            {
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(() => ClaimQuestReward(quest));
            }
        }
        
        // Cập nhật reward text - sửa index từ 3 thành 4
        if (texts.Length >= 4)
        {
            if (quest.reward != null)
            {
                if (quest.state == Quest.QuestState.Rewarded)
                {
                    texts[3].text = "Reward Claimed";
                }
                else if (quest.state == Quest.QuestState.Completed)
                {
                    texts[3].text = $"Reward: {quest.reward.amount} {quest.reward.rewardType}";
                }
                else
                {
                    texts[3].text = $"Reward: {quest.reward.amount} {quest.reward.rewardType}";
                }
            }
            else
            {
                texts[3].text = "No Reward Set";
            }
        }
        switch (quest.state)
        {
            case Quest.QuestState.Locked:
                texts[2].text = "🔒 Locked";
                break;
            case Quest.QuestState.Available:
                texts[2].text = "✅ Available";
                break;
            case Quest.QuestState.InProgress:
                texts[2].text = "⏳ In Progress";
                break;
            case Quest.QuestState.Completed:
                texts[2].text = "🎉 Completed";
                break;
            case Quest.QuestState.Rewarded:
                texts[2].text = "💰 Rewarded";
                break;
            default:
                texts[2].text = "Unknown";
                break;
        }
    }

    private Color GetStateColor(Quest.QuestState state)
    {
        switch (state)
        {
            case Quest.QuestState.Locked:
                return lockedColor;
            case Quest.QuestState.Available:
                return availableColor;
            case Quest.QuestState.InProgress:
                return inProgressColor;
            case Quest.QuestState.Completed:
                return completedColor;
            case Quest.QuestState.Rewarded:
                return rewardedColor;
            default:
                return Color.white;
        }
    }

    private float GetQuestProgress(Quest quest)
    {
        return quest.GetProgressPercentage() / 100f;
    }


    private void ClaimQuestReward(Quest quest)
    {
        quest.ClaimReward();
        UpdateQuestState(quest);
    }

    // Phương thức để refresh tất cả quest
    public void RefreshAllQuests()
    {
        foreach (var kvp in questData)
        {
            UpdateQuestState(kvp.Value);
        }
    }

    // Phương thức để xóa quest khỏi UI
    public void RemoveQuest(int questID)
    {
        if (questItems.ContainsKey(questID))
        {
            Destroy(questItems[questID]);
            questItems.Remove(questID);
            questData.Remove(questID);
        }
    }
}
