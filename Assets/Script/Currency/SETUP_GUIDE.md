# Hướng dẫn Setup Hệ thống Tiền tệ (Thủ công)

## 1. Setup CurrencyManager (BẮT BUỘC)

### Bước 1: Tạo CurrencyManager GameObject

1. **Tạo Empty GameObject** trong scene chính
2. **Đặt tên:** "CurrencyManager"
3. **Add Component:** `CurrencyManager`
4. **Cấu hình trong Inspector:**
   - Starting Coins: 100 (số tiền xu ban đầu)
   - Starting Gems: 10 (số đá quý ban đầu)
5. **Tích "Don't Destroy On Load"** nếu muốn giữ qua scene

### Bước 2: Gán vào GameManager (TUỲ CHỌN)

1. Chọn GameManager GameObject
2. Trong Inspector, tìm mục "System References"
3. Kéo CurrencyManager GameObject vào slot "Currency Manager"

## 2. Setup UI hiển thị tiền tệ (BẮT BUỘC)

### Bước 1: Tạo UI Elements

1. **Tạo Canvas** nếu chưa có (GameObject → UI → Canvas)
2. **Tạo Panel** cho Currency Display:
   - Right-click Canvas → UI → Panel
   - Đặt tên: "CurrencyPanel"
   - Đặt ở góc trên cùng màn hình
3. **Tạo Text cho Coins:**
   - Right-click CurrencyPanel → UI → Text
   - Đặt tên: "CoinsText"
   - Text: "Coins: 0"
4. **Tạo Text cho Gems:**
   - Right-click CurrencyPanel → UI → Text
   - Đặt tên: "GemsText"
   - Text: "Gems: 0"

### Bước 2: Setup CurrencyUI Component

1. **Tạo Empty GameObject** trong Canvas
2. **Đặt tên:** "CurrencyUI"
3. **Add Component:** `CurrencyUI`
4. **Gán trong Inspector:**
   - Coins Text → kéo CoinsText vào
   - Gems Text → kéo GemsText vào
   - Use Animation → false (tắt hiệu ứng để đơn giản)

### Bước 3: Tùy chỉnh UI (TUỲ CHỌN)

1. **Thêm Icons:** Kéo sprite icon vào Coins Icon và Gems Icon (nếu có)
2. **Styling:** Đổi font, màu sắc, kích thước text 

## 2. Sử dụng trong Code

### Thêm tiền tệ:

```csharp
// Thêm coins
CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, 50);

// Thêm gems
CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, 5);
```

### Trừ tiền tệ (mua đồ):

```csharp
// Mua bằng coins
bool success = CurrencyManager.Instance.SpendCurrency(CurrencyType.Coins, 100);
if (success) {
    // Mua thành công
} else {
    // Không đủ tiền
}
```

### Kiểm tra tiền:

```csharp
// Kiểm tra có đủ tiền không
bool canBuy = CurrencyManager.Instance.HasEnoughCurrency(CurrencyType.Coins, 100);

// Lấy số tiền hiện tại
int currentCoins = CurrencyManager.Instance.GetCurrency(CurrencyType.Coins);
```

## 4. Setup Quest với phần thưởng tiền tệ

### Cách 1: Setup trong ScriptableObject (KHUYẾN KHÍCH)

1. **Mở Quest asset** trong Project window
2. **Trong Inspector,** tìm mục "Currency Reward"
3. **Expand Currency Reward** → Expand "Rewards"
4. **Set Size:** 1 (hoặc 2 nếu muốn cho cả Coins và Gems)
5. **Element 0:**
   - Type: Coins
   - Amount: 50
6. **Element 1 (nếu có):**
   - Type: Gems
   - Amount: 2

### Cách 2: Setup trong Code (CHO QUEST ĐỘNG)

```csharp
// Trong quest script
void SetupQuestReward() {
    if (currencyReward == null) {
        currencyReward = new CurrencyReward();
    }
    currencyReward.AddReward(CurrencyType.Coins, 100);
    currencyReward.AddReward(CurrencyType.Gems, 5);
}
```

### Hoặc setup trong code:

```csharp
// Trong quest class
void SetupReward() {
    currencyReward = new CurrencyReward();
    currencyReward.AddReward(CurrencyType.Coins, 50);
    currencyReward.AddReward(CurrencyType.Gems, 2);
}
```

## 5. Setup Building với chi phí tiền tệ

### Cách 1: Setup trong ScriptableObject (KHUYẾN KHÍCH)

1. **Mở Building asset** trong Project window
2. **Trong Inspector,** tìm mục "Currency Costs"
3. **Expand Currency Costs** → Expand "Rewards" (đây là costs, không phải rewards)
4. **Set Size:** 1 (hoặc 2 nếu cần cả Coins và Gems)
5. **Element 0:**
   - Type: Coins
   - Amount: 200
6. **Element 1 (nếu có):**
   - Type: Gems
   - Amount: 10

### Cách 2: Setup trong Code (CHO BUILDING ĐỘNG)

```csharp
// Trong building script hoặc building manager
void SetupBuildingCost(Building building) {
    if (building.currencyCosts == null) {
        building.currencyCosts = new CurrencyReward();
    }
    building.currencyCosts.AddReward(CurrencyType.Coins, 500);
    building.currencyCosts.AddReward(CurrencyType.Gems, 25);
}
```

### Sử dụng trong BuildingManager:

```csharp
void TryBuildBuilding(Building building) {
    if (building.CanAfford()) {
        if (building.Purchase()) {
            // Xây dựng thành công
            Debug.Log("Đã xây dựng: " + building.buildingName);
        }
    } else {
        Debug.Log("Không đủ tiền!");
    }
}
```

## 5. Lắng nghe Events (Optional)

```csharp
void OnEnable() {
    CurrencyManager.OnCurrencyChanged += OnCurrencyChanged;
    CurrencyManager.OnCurrencyAdded += OnCurrencyAdded;
}

void OnDisable() {
    CurrencyManager.OnCurrencyChanged -= OnCurrencyChanged;
    CurrencyManager.OnCurrencyAdded -= OnCurrencyAdded;
}

void OnCurrencyChanged(CurrencyType type, int oldAmount, int newAmount) {
    Debug.Log($"{type} thay đổi từ {oldAmount} thành {newAmount}");
}

void OnCurrencyAdded(CurrencyType type, int amount) {
    Debug.Log($"Nhận được {amount} {type}");
}
```

## 6. Các tình huống sử dụng phổ biến

### Reward từ Achievement:

```csharp
// Trong Achievement system
void GiveAchievementReward() {
    CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, 10);
}
```

### Reward từ Daily Login:

```csharp
void GiveDailyReward() {
    CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, 100);
    CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, 1);
}
```

### Bán Slime:

```csharp
void SellSlime(Slime slime) {
    int sellPrice = CalculateSlimePrice(slime);
    CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, sellPrice);
}
```

### Breeding cost:

```csharp
void TryBreeding() {
    int breedingCost = 50;
    if (CurrencyManager.Instance.SpendCurrency(CurrencyType.Coins, breedingCost)) {
        // Thực hiện breeding
    }
}
```

## 7. Checklist Setup hoàn chỉnh

### ✅ BẮT BUỘC:

- [ ] Tạo CurrencyManager GameObject trong scene
- [ ] Tạo CurrencyUI với Text components
- [ ] Gán Text vào CurrencyUI component
- [ ] Test bằng cách gọi AddCurrency trong code

### ✅ TUỲ CHỌN:

- [ ] Gán CurrencyManager vào GameManager
- [ ] Setup Quest rewards trong ScriptableObjects
- [ ] Setup Building costs trong ScriptableObjects
- [ ] Thêm currency icons vào UI

## 8. Tips quan trọng

1. **PHẢI tạo CurrencyManager thủ công** - Không tự động tạo
2. **Dữ liệu tự động lưu** - Sử dụng PlayerPrefs
3. **UI tự động cập nhật** - Nhờ event system
4. **Thread-safe** - An toàn khi gọi từ nhiều nơi
5. **Có thể sửa đổi** - Tất cả settings có thể thay đổi trong Inspector
6. **Reset dễ dàng** - Dùng `CurrencyManager.Instance.ResetAllCurrency()`

## 8. Troubleshooting

### Nếu UI không cập nhật:

- Kiểm tra CurrencyUI đã gán đúng Text chưa
- Kiểm tra CurrencyUI có trong scene không

### Nếu tiền không lưu:

- Kiểm tra PlayerPrefs có quyền ghi không
- Thử gọi `PlayerPrefs.Save()` thủ công

### Nếu CurrencyManager null:

- **QUAN TRỌNG:** Kiểm tra đã tạo CurrencyManager GameObject chưa
- Kiểm tra CurrencyManager component đã được add chưa
- Kiểm tra GameManager có gán CurrencyManager không (nếu dùng)

### Nếu Console báo warning:

- "CurrencyManager không tìm thấy trong scene!" → Tạo CurrencyManager GameObject
- Các warning khác → Kiểm tra Inspector settings

## 9. Bước tiếp theo

Sau khi setup xong, bạn có thể:

1. **Tạo shop system** sử dụng currency costs
2. **Thêm daily rewards** bằng CurrencyReward
3. **Tích hợp vào achievement system**
4. **Tạo currency exchange** (đổi coins thành gems)
5. **Thêm currency multipliers** (x2 coins events)

Hệ thống đã sẵn sàng cho setup thủ công!
