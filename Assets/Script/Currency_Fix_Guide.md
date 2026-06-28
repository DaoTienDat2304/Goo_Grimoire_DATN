# 🔧 Hướng Dẫn Sửa Lỗi Currency System

## ✅ **Đã sửa các vấn đề:**

### 1. **Achievement System** - Đã tích hợp currency rewards

- ✅ Thêm `AchievementRewardHolder` component
- ✅ Cập nhật `AchievementManager` để thưởng currency khi unlock achievement
- ✅ Cập nhật `Archievement` class để có method `GiveCurrencyReward`

### 2. **Building System** - Đã sửa để trừ tiền khi xây dựng

- ✅ Cập nhật `BuildingSlot.PlaceBuilding()` để gọi `building.Purchase()`
- ✅ Thêm kiểm tra `building.CanAfford()` trước khi xây dựng
- ✅ Thêm debug log để theo dõi quá trình

---

## 🎯 **Cách Test Hệ Thống:**

### **Test Achievement Rewards:**

1. **Setup Achievement với Currency Reward:**

   - Mở Achievement asset (1.asset, 2.asset, etc.)
   - Trong Inspector, tìm **"Currency Rewards"**
   - **Expand "Currency Reward"** → **"Rewards"**
   - **Set Size: 1**
   - **Element 0:**
     - **Type:** Coins
     - **Amount:** 50

2. **Test Achievement:**
   - Chạy game
   - Làm action để unlock achievement (ví dụ: breed slime 3 lần)
   - Kiểm tra Console log: `"Achievement 'Breed' đã thưởng: 50 Coins"`
   - Kiểm tra coins có tăng không

### **Test Building Purchase:**

1. **Setup Building với Currency Cost:**

   - Mở Building asset (BreedCave, DivineOozeSanctum, etc.)
   - Trong Inspector, tìm **"Currency Costs"**
   - **Expand "Currency Costs"** → **"Rewards"**
   - **Set Size: 1**
   - **Element 0:**
     - **Type:** Coins
     - **Amount:** 100

2. **Test Building:**
   - Chạy game
   - Mở building menu (nhấn B)
   - Kéo building vào slot
   - Kiểm tra Console log: `"Đã xây dựng [BuildingName] với chi phí: 100 Coins"`
   - Kiểm tra coins có bị trừ không

---

## 🚨 **Troubleshooting:**

### **Achievement không thưởng currency:**

1. **Kiểm tra Achievement asset có currencyReward không:**
   - Mở asset → Currency Rewards → Currency Reward → Rewards → Size > 0
2. **Kiểm tra Console log:**
   - Có thấy `"Achievement '[Name]' đã thưởng: [Amount] [Type]"` không?
3. **Kiểm tra CurrencyManager:**
   - Có GameObject "CurrencyManager" trong scene không?
   - Component CurrencyManager có được gán không?

### **Building không trừ tiền:**

1. **Kiểm tra Building asset có currencyCosts không:**
   - Mở asset → Currency Costs → Rewards → Size > 0
2. **Kiểm tra Console log:**
   - Có thấy `"Đã xây dựng [Name] với chi phí: [Amount] [Type]"` không?
   - Có thấy `"Không thể xây dựng [Name]: Không đủ tiền!"` không?
3. **Kiểm tra CurrencyManager:**
   - Có đủ tiền để mua building không?

### **Currency UI không cập nhật:**

1. **Kiểm tra CurrencyUI component:**
   - Có GameObject "CurrencyUI" trong scene không?
   - Component CurrencyUI có được gán Coins Text và Gems Text không?
2. **Kiểm tra CurrencyManager events:**
   - CurrencyManager có gọi events khi thay đổi currency không?

---

## 📋 **Checklist Test:**

### **Achievement System:**

- [ ] Achievement asset có currencyReward setup
- [ ] Làm action để unlock achievement
- [ ] Console log hiển thị thông báo thưởng
- [ ] Coins/Gems tăng đúng số lượng
- [ ] UI cập nhật hiển thị số tiền mới

### **Building System:**

- [ ] Building asset có currencyCosts setup
- [ ] Có đủ tiền để mua building
- [ ] Kéo building vào slot
- [ ] Console log hiển thị thông báo mua thành công
- [ ] Coins/Gems bị trừ đúng số lượng
- [ ] UI cập nhật hiển thị số tiền mới

### **Quest System (đã hoạt động):**

- [ ] Quest asset có currencyReward setup
- [ ] Hoàn thành quest
- [ ] Click "Claim Reward"
- [ ] Coins/Gems tăng đúng số lượng

---

## 💡 **Tips Debug:**

### **Sử dụng Console Log:**

- Mở Console window (Window → General → Console)
- Theo dõi các log message để biết hệ thống hoạt động như thế nào

### **Sử dụng Debug Commands:**

- Trong CurrencyManager, có Context Menu để thêm tiền
- Right-click CurrencyManager component → "Add 100 Coins" hoặc "Add 10 Gems"

### **Kiểm tra PlayerPrefs:**

- Currency được lưu trong PlayerPrefs với key `"Currency_Coins"` và `"Currency_Gems"`
- Có thể xóa để reset: PlayerPrefs.DeleteKey("Currency_Coins")

---

## 🎉 **Kết quả mong đợi:**

Sau khi setup và test đúng:

- ✅ **Quest hoàn thành** → Nhận coins/gems
- ✅ **Achievement unlock** → Nhận coins/gems
- ✅ **Building xây dựng** → Trừ coins/gems
- ✅ **UI cập nhật** → Hiển thị số tiền chính xác
- ✅ **Dữ liệu lưu** → Tiền được lưu giữa các session

**Chúc bạn test thành công! 🚀**
