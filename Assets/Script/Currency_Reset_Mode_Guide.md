# 🔄 Hướng Dẫn Chế Độ Currency Reset

## ✅ **Đã sửa: Currency luôn reset khi nhấn Play**

Bây giờ mỗi khi bạn nhấn **Play** trong Unity, tiền sẽ luôn reset về giá trị ban đầu đã set trong CurrencyManager.

---

## 🎯 **Cách hoạt động hiện tại:**

### **Mỗi lần nhấn Play:**

- ✅ Coins = 100 (giá trị ban đầu)
- ✅ Gems = 10 (giá trị ban đầu)
- ✅ Không lưu tiền đã tiêu từ lần chơi trước

### **Trong 1 session chơi:**

- ✅ Quest hoàn thành → Nhận coins/gems
- ✅ Achievement unlock → Nhận coins/gems
- ✅ Building xây dựng → Trừ coins/gems
- ✅ Tiền thay đổi bình thường

### **Khi tắt game và nhấn Play lại:**

- ✅ Tiền reset về 100 coins, 10 gems
- ✅ Bắt đầu lại từ đầu

---

## ⚙️ **Nếu muốn thay đổi chế độ:**

### **Chế độ 1: Reset mỗi lần Play (HIỆN TẠI)**

```csharp
// Trong CurrencyManager.InitializeCurrencies()
currencies[CurrencyType.Coins] = startingCoins;
currencies[CurrencyType.Gems] = startingGems;
// LoadCurrencyData(); // Đã comment out
```

### **Chế độ 2: Lưu tiền giữa các lần chơi**

```csharp
// Trong CurrencyManager.InitializeCurrencies()
currencies[CurrencyType.Coins] = 0;
currencies[CurrencyType.Gems] = 0;
LoadCurrencyData(); // Uncomment dòng này
if (!PlayerPrefs.HasKey("Currency_Coins"))
{
    currencies[CurrencyType.Coins] = startingCoins;
}
if (!PlayerPrefs.HasKey("Currency_Gems"))
{
    currencies[CurrencyType.Gems] = startingGems;
}
```

---

## 🎮 **Test chế độ hiện tại:**

### **Test 1: Reset khi Play**

1. **Chạy game lần 1:**

   - Kiểm tra coins = 100, gems = 10
   - Hoàn thành quest → nhận thêm coins
   - Xây building → trừ coins
   - Tắt game

2. **Chạy game lần 2:**
   - Kiểm tra coins = 100, gems = 10 (đã reset)
   - Console log: `"Currency reset về giá trị ban đầu: 100 Coins, 10 Gems"`

### **Test 2: Hoạt động trong session**

1. **Chạy game:**
   - Coins = 100, Gems = 10
   - Hoàn thành quest → Coins tăng
   - Unlock achievement → Coins/Gems tăng
   - Xây building → Coins/Gems giảm
   - Tiền thay đổi bình thường trong session

---

## 💡 **Lợi ích của chế độ reset:**

### **Cho Development/Testing:**

- ✅ Luôn bắt đầu với số tiền cố định
- ✅ Dễ test các tính năng mới
- ✅ Không bị ảnh hưởng bởi dữ liệu cũ

### **Cho Gameplay:**

- ✅ Mỗi lần chơi là một trải nghiệm mới
- ✅ Không bị tích lũy tiền từ các lần chơi trước
- ✅ Cân bằng game tốt hơn

---

## 🔧 **Tùy chỉnh giá trị ban đầu:**

### **Thay đổi trong Inspector:**

1. **Chọn CurrencyManager GameObject**
2. **Trong Inspector:**
   - **Starting Coins:** 100 → đổi thành số khác
   - **Starting Gems:** 10 → đổi thành số khác

### **Thay đổi trong Code:**

```csharp
[Header("Starting Currency")]
[SerializeField] private int startingCoins = 100; // Đổi số này
[SerializeField] private int startingGems = 10;   // Đổi số này
```

---

## 📋 **Checklist Test:**

### **Chế độ Reset (hiện tại):**

- [ ] Game lần 1: coins = 100, gems = 10
- [ ] Thêm/trừ tiền trong game
- [ ] Tắt game
- [ ] Game lần 2: coins = 100, gems = 10 (reset)
- [ ] Console log hiển thị "Currency reset về giá trị ban đầu"

### **Achievement vẫn hoạt động:**

- [ ] Breed slime 3 lần → achievement unlock
- [ ] Nhận coins từ achievement
- [ ] Achievement chuyển màu vàng

### **Building vẫn hoạt động:**

- [ ] Xây building → trừ coins
- [ ] Console log hiển thị chi phí
- [ ] Building được xây thành công

---

## 🎉 **Kết quả:**

Bây giờ hệ thống sẽ hoạt động như bạn mong muốn:

- ✅ **Mỗi lần Play** → Tiền reset về 100 coins, 10 gems
- ✅ **Achievement** → Vẫn hoạt động và thưởng tiền
- ✅ **Building** → Vẫn trừ tiền khi xây dựng
- ✅ **Quest** → Vẫn thưởng tiền khi hoàn thành
- ✅ **Trong session** → Tiền thay đổi bình thường

**Chúc bạn chơi game vui vẻ! 🚀**
