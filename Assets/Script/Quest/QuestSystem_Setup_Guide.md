# Quest System Setup Guide

## Tổng quan
Hệ thống quest đã được hoàn thiện với các tính năng:
- ✅ Hiển thị progress bar cho TimeQuest và BreedingQuest
- ✅ Trạng thái quest với màu sắc và icon
- ✅ Tự động refresh UI khi quest state thay đổi
- ✅ Hiển thị reward và nút claim reward
- ✅ QuestManager để quản lý toàn bộ hệ thống

## Cấu trúc Quest System

### 1. Quest Base Class
- `Quest.cs`: Lớp cơ sở cho tất cả quest
- `QuestReward`: Class chứa thông tin reward
- Các trạng thái: Locked, Available, InProgress, Completed, Rewarded

### 2. Quest Types
- `TimeQuest.cs`: Quest theo thời gian
- `BreedingQuest.cs`: Quest về số lượng slime

### 3. UI Components
- `QuestUIManager.cs`: Quản lý hiển thị quest trên UI
- `QuestLogToggle.cs`: Điều khiển mở/đóng quest log
- `QuestManager.cs`: Quản lý toàn bộ quest system

## Setup Instructions

### 1. Tạo QuestManager GameObject
1. Tạo empty GameObject trong scene
2. Đặt tên "QuestManager"
3. Add component `QuestManager`
4. Kéo các quest assets vào field "All Quests"
5. Assign các references:
   - QuestUIManager
   - QuestLogToggle
   - BreedingManager

### 2. Setup Quest UI
1. Tạo Canvas cho Quest UI
2. Tạo ScrollView với Content
3. Tạo QuestItem prefab với các components:
   - Image (background)
   - 5x TMP_Text (name, description, state, progress, reward)
   - Slider (progress bar)
   - Button (claim reward)
4. Assign QuestItem prefab vào QuestUIManager

### 3. Setup Quest Assets
1. Tạo TimeQuest asset:
   - Right-click → Create → Quests → Time Quest
   - Set questID, questName, description
   - Set slimeRequirement, questreq
   - Set reward (type, amount, description)
   - Set required time

2. Tạo BreedingQuest asset:
   - Right-click → Create → Quests → Breeding Quest
   - Set questID, questName, description
   - Set slimeRequirement, questreq
   - Set reward (type, amount, description)
   - Set slimeGoal

### 4. Quest Item Prefab Structure
```
QuestItem
├── Image (Background)
├── TMP_Text (Quest Name) - texts[0]
├── TMP_Text (Description) - texts[1]
├── TMP_Text (State) - texts[2]
├── TMP_Text (Progress) - texts[3]
├── TMP_Text (Reward) - texts[4]
├── Slider (Progress Bar)
└── Button (Claim Reward)
```

## Sử dụng

### 1. Thêm Quest mới
```csharp
// Trong QuestManager
Quest newQuest = ScriptableObject.CreateInstance<TimeQuest>();
newQuest.questID = 2;
newQuest.questName = "New Quest";
newQuest.description = "Complete this quest";
newQuest.slimeRequirement = 5;
newQuest.reward = new QuestReward { rewardType = "coins", amount = 100 };
QuestManager.Instance.AddQuest(newQuest);
```

### 2. Kiểm tra trạng thái quest
```csharp
Quest quest = QuestManager.Instance.GetQuest(questID);
if (quest.state == Quest.QuestState.Completed)
{
    // Quest đã hoàn thành
}
```

### 3. Lấy danh sách quest theo trạng thái
```csharp
List<Quest> activeQuests = QuestManager.Instance.GetQuestsByState(Quest.QuestState.InProgress);
```

## Tính năng mới

### 1. Progress Display
- TimeQuest: Hiển thị "X.Xs / Ys (Z%)"
- BreedingQuest: Hiển thị "X / Y slimes (Z%)"
- Progress bar với màu sắc tương ứng

### 2. Visual States
- 🔒 Locked (Gray)
- ✅ Available (White)
- ⏳ In Progress (Yellow)
- 🎉 Completed (Green)
- 💰 Rewarded (Blue)

### 3. Reward System
- Hiển thị reward khi quest available
- Nút claim reward khi quest completed
- Thông báo khi đã claim reward

### 4. Auto Management
- Tự động unlock quest khi đủ điều kiện
- Tự động start quest khi available
- Tự động complete quest khi đạt mục tiêu
- Tự động refresh UI

## Migration từ QuestTestSpawner

Nếu bạn đang sử dụng QuestTestSpawner, có thể:
1. Thay thế QuestTestSpawner bằng QuestManager
2. Move quest list từ QuestTestSpawner sang QuestManager
3. Assign các references tương ứng

QuestManager sẽ tự động xử lý tất cả logic quest mà QuestTestSpawner đang làm.

