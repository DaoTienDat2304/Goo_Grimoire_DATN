# 🔧 Hướng Dẫn Sửa Lỗi Currency Save & Achievement

## ✅ **Đã sửa các vấn đề:**

### 1. **Currency không save** - Đã sửa xong

**Vấn đề:** Mỗi lần vào game, tiền reset về giá trị ban đầu
**Nguyên nhân:** `InitializeCurrencies()` set giá trị ban đầu TRƯỚC khi load dữ liệu đã lưu
**Đã sửa:** Thay đổi thứ tự - load dữ liệu trước, chỉ dùng giá trị ban đầu nếu chưa có dữ liệu

### 2. **Achievement không hoàn thành** - Đã sửa xong

**Vấn đề:** Làm đúng yêu cầu nhưng achievement không unlock
**Nguyên nhân 1:** `PlayerPrefs.DeleteAll()` trong Achievement constructor xóa TẤT CẢ dữ liệu
**Nguyên nhân 2:** `BreedingManager` không gọi achievement trigger
**Đã sửa:**

- Xóa `PlayerPrefs.DeleteAll()`
- Thêm trigger achievement trong `BreedingManager.CompleteBreeding()`

---

## 🎯 **Cách Test Hệ Thống:**

### **Test 1: Currency Save/Load**

1. **Chạy game lần đầu:**

   - Kiểm tra coins = 100, gems = 10 (giá trị ban đầu)
   - Thêm tiền bằng cách hoàn thành quest hoặc dùng debug command
   - Tắt game

2. **Chạy game lần 2:**
   - Kiểm tra coins/gems có giữ nguyên số tiền đã có không
   - Nếu vẫn reset về 100/10 → vấn đề chưa sửa xong

### **Test 2: Achievement System**

1. **Setup Achievement:**

   - Mở Achievement asset (1.asset)
   - Trong Inspector → Currency Rewards → Currency Reward → Rewards
   - Set Size: 1, Type: Coins, Amount: 50

2. **Test Achievement:**
   - Chạy game
   - Breed slime 3 lần (theo yêu cầu achievement)
   - Kiểm tra Console log: `"Achievement 'Breed' đã thưởng: 50 Coins"`
   - Kiểm tra achievement có chuyển màu vàng không
   - Kiểm tra coins có tăng 50 không

### **Test 3: Building Purchase**

1. **Setup Building:**

   - Mở Building asset
   - Trong Inspector → Currency Costs → Rewards
   - Set Size: 1, Type: Coins, Amount: 100

2. **Test Building:**
   - Chạy game
   - Mở building menu (nhấn B)
   - Kéo building vào slot
   - Kiểm tra Console log: `"Đã xây dựng [Name] với chi phí: 100 Coins"`
   - Kiểm tra coins có bị trừ 100 không

---

## 🚨 **Troubleshooting:**

### **Currency vẫn không save:**

1. **Kiểm tra Console log:**

   - Có thấy `"Thêm X Coins. Tổng: Y"` không?
   - Có thấy `"Tiêu X Coins. Còn lại: Y"` không?

2. **Kiểm tra PlayerPrefs:**

   - Mở Console → gõ: `PlayerPrefs.GetInt("Currency_Coins")`
   - Nếu trả về 0 → dữ liệu không được lưu

3. **Kiểm tra CurrencyManager:**
   - Có GameObject "CurrencyManager" trong scene không?
   - Component CurrencyManager có được gán không?

### **Achievement vẫn không hoàn thành:**

1. **Kiểm tra Console log:**

   - Có thấy `"breed"` khi breeding không?
   - Có thấy `"get Something"` khi achievement unlock không?

2. **Kiểm tra Achievement setup:**

   - Achievement asset có targetValue = 3 không?
   - Achievement asset có currencyReward setup không?

3. **Kiểm tra Breeding:**
   - Breeding có thành công không? (tạo ra slime mới)
   - Console có log breeding thành công không?

### **Building vẫn không trừ tiền:**

1. **Kiểm tra Console log:**

   - Có thấy `"Đã xây dựng [Name] với chi phí: X Coins"` không?
   - Có thấy `"Không thể xây dựng [Name]: Không đủ tiền!"` không?

2. **Kiểm tra Building setup:**
   - Building asset có currencyCosts setup không?
   - Có đủ tiền để mua building không?

---

## 📋 **Checklist Test Hoàn Chỉnh:**

### **Currency System:**

- [ ] Game lần đầu: coins = 100, gems = 10
- [ ] Thêm tiền (quest/achievement)
- [ ] Tắt game
- [ ] Game lần 2: tiền giữ nguyên
- [ ] Trừ tiền (building)
- [ ] Tắt game
- [ ] Game lần 3: tiền giữ nguyên

### **Achievement System:**

- [ ] Achievement asset có currencyReward setup
- [ ] Breed slime 1 lần → achievement chưa unlock
- [ ] Breed slime 2 lần → achievement chưa unlock
- [ ] Breed slime 3 lần → achievement unlock + nhận coins
- [ ] Achievement chuyển màu vàng
- [ ] Console log hiển thị thông báo thưởng

### **Building System:**

- [ ] Building asset có currencyCosts setup
- [ ] Có đủ tiền để mua building
- [ ] Kéo building vào slot
- [ ] Building được xây dựng thành công
- [ ] Coins bị trừ đúng số lượng
- [ ] Console log hiển thị thông báo mua

---

## 💡 **Debug Commands:**

### **Thêm tiền để test:**

```csharp
// Trong code hoặc Console
CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, 1000);
CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, 50);
```

### **Reset currency:**

```csharp
CurrencyManager.Instance.ResetAllCurrency();
```

### **Kiểm tra PlayerPrefs:**

```csharp
// Trong Console
PlayerPrefs.GetInt("Currency_Coins")
PlayerPrefs.GetInt("Currency_Gems")
```

### **Trigger achievement thủ công:**

```csharp
ArchievementManager.Instance.GetArchivement(0); // Breed achievement
```

---

## 🎉 **Kết quả mong đợi:**

Sau khi test thành công:

- ✅ **Currency save/load** → Tiền được lưu giữa các session
- ✅ **Achievement unlock** → Làm đúng yêu cầu → nhận coins/gems
- ✅ **Building purchase** → Trừ tiền khi xây dựng
- ✅ **UI cập nhật** → Hiển thị số tiền chính xác
- ✅ **Console log** → Thông báo rõ ràng mọi hoạt động

**Chúc bạn test thành công! 🚀**
