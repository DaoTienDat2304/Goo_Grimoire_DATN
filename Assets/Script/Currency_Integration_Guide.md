# 🎯 Hướng Dẫn Tích Hợp Coins & Gems Vào Hệ Thống Hiện Có

## ✅ **Hệ thống đã có sẵn:**

- ✅ CurrencyManager - Quản lý coins/gems
- ✅ CurrencyReward - Hệ thống phần thưởng
- ✅ Quest system với currencyReward field
- ✅ Building system với currencyCosts field
- ✅ Achievement system (cần setup thêm)

---

## 🎮 **Bước 1: Setup Quest với phần thưởng Coins/Gems**

### Cách setup trong Quest Asset:

1. **Mở Quest asset** (TimeQuest, BreedingQuest, etc.)
2. **Trong Inspector**, tìm mục **"Currency Reward"**
3. **Expand "Currency Reward"** → **"Rewards"**
4. **Set Size: 1** (hoặc 2 nếu muốn cả Coins và Gems)
5. **Element 0:**
   - **Type:** Coins
   - **Amount:** 50 (số coins muốn thưởng)
6. **Element 1** (nếu muốn thêm Gems):
   - **Type:** Gems
   - **Amount:** 2

### Ví dụ Quest Rewards:

- **Quest đầu tiên:** 50 Coins
- **Quest khó:** 100 Coins + 5 Gems
- **Quest đặc biệt:** 10 Gems

---

## 🏆 **Bước 2: Setup Achievement với phần thưởng Coins/Gems**

### Cách setup Achievement Asset:

1. **Mở Achievement asset** (1.asset, 2.asset, etc.)
2. **Trong Inspector**, tìm mục **"Currency Rewards"**
3. **Expand "Currency Reward"** → **"Rewards"**
4. **Set Size: 1** (hoặc 2)
5. **Element 0:**
   - **Type:** Coins
   - **Amount:** 100
6. **Element 1** (nếu muốn thêm Gems):
   - **Type:** Gems
   - **Amount:** 3

### Ví dụ Achievement Rewards:

- **Breed 3 times:** 50 Coins
- **Breed 5 times:** 100 Coins + 2 Gems
- **Complete 10 quests:** 5 Gems

---

## 🏗️ **Bước 3: Setup Building với chi phí Coins/Gems**

### Cách setup Building Asset:

1. **Mở Building asset** (BreedCave, DivineOozeSanctum, etc.)
2. **Trong Inspector**, tìm mục **"Currency Costs"**
3. **Expand "Currency Costs"** → **"Rewards"** (đây là costs, không phải rewards)
4. **Set Size: 1** (hoặc 2)
5. **Element 0:**
   - **Type:** Coins
   - **Amount:** 200 (chi phí xây dựng)
6. **Element 1** (nếu cần Gems):
   - **Type:** Gems
   - **Amount:** 5

### Ví dụ Building Costs:

- **BreedCave:** 100 Coins
- **DivineOozeSanctum:** 500 Coins + 10 Gems
- **Advanced Building:** 20 Gems

---

## 🎨 **Bước 4: Setup UI hiển thị Currency (Nếu chưa có)**

### Tạo Currency Display UI:

1. **Tạo Canvas** nếu chưa có
2. **Tạo Panel** cho Currency:
   - Right-click Canvas → UI → Panel
   - Đặt tên: "CurrencyPanel"
   - Đặt ở góc trên màn hình
3. **Tạo Text cho Coins:**
   - Right-click CurrencyPanel → UI → Text - TextMeshPro
   - Đặt tên: "CoinsText"
   - Text: "Coins: 0"
4. **Tạo Text cho Gems:**
   - Right-click CurrencyPanel → UI → Text - TextMeshPro
   - Đặt tên: "GemsText"
   - Text: "Gems: 0"

### Setup CurrencyUI Component:

1. **Tạo Empty GameObject** trong Canvas
2. **Đặt tên:** "CurrencyUI"
3. **Add Component:** `CurrencyUI`
4. **Gán trong Inspector:**
   - Coins Text → kéo CoinsText vào
   - Gems Text → kéo GemsText vào

---

## 🔧 **Bước 5: Test hệ thống**

### Test Quest Rewards:

1. **Hoàn thành quest** trong game
2. **Click "Claim Reward"**
3. **Kiểm tra** coins/gems có tăng không

### Test Achievement Rewards:

1. **Làm action** để unlock achievement
2. **Kiểm tra** coins/gems có tăng không

### Test Building Costs:

1. **Thử xây building**
2. **Kiểm tra** coins/gems có bị trừ không
3. **Kiểm tra** có đủ tiền để xây không

---

## 💡 **Tips và Lưu ý:**

### Quest Rewards:

- **Quest dễ:** 20-50 Coins
- **Quest trung bình:** 50-100 Coins
- **Quest khó:** 100+ Coins hoặc 1-5 Gems
- **Quest đặc biệt:** 5-10 Gems

### Achievement Rewards:

- **Achievement đơn giản:** 50-100 Coins
- **Achievement khó:** 100+ Coins hoặc 2-5 Gems
- **Achievement hiếm:** 5-10 Gems

### Building Costs:

- **Building cơ bản:** 50-200 Coins
- **Building trung bình:** 200-500 Coins
- **Building cao cấp:** 500+ Coins hoặc 5-20 Gems
- **Building đặc biệt:** 10-50 Gems

### Cân bằng Game:

- **Coins:** Dễ kiếm, dùng cho mua đồ thường
- **Gems:** Khó kiếm, dùng cho mua đồ đặc biệt
- **Tỷ lệ:** 1 Gem = 50-100 Coins

---

## 🚀 **Sử dụng trong Code:**

### Thêm tiền tệ:

```csharp
// Thêm coins
CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, 50);

// Thêm gems
CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, 2);
```

### Trừ tiền tệ:

```csharp
// Trừ coins
bool success = CurrencyManager.Instance.SpendCurrency(CurrencyType.Coins, 100);

// Trừ gems
bool success = CurrencyManager.Instance.SpendCurrency(CurrencyType.Gems, 5);
```

### Kiểm tra tiền:

```csharp
// Kiểm tra có đủ tiền không
bool canBuy = CurrencyManager.Instance.HasEnoughCurrency(CurrencyType.Coins, 100);

// Lấy số tiền hiện tại
int currentCoins = CurrencyManager.Instance.GetCurrency(CurrencyType.Coins);
```

---

## ✅ **Checklist hoàn thành:**

- [ ] Quest assets có currencyReward setup
- [ ] Achievement assets có currencyReward setup
- [ ] Building assets có currencyCosts setup
- [ ] UI hiển thị currency hoạt động
- [ ] Test quest rewards
- [ ] Test achievement rewards
- [ ] Test building costs
- [ ] Cân bằng số lượng coins/gems

**Chúc bạn setup thành công! 🎉**
