# SlimeAI Siêu Khó Bắt - Hướng Dẫn Sử Dụng

## 🎯 Tổng Quan

SlimeAI đã được cải tiến hoàn toàn để tạo ra con slime **CỰC KỲ KHÓ BẮT** với các tính năng di chuyển thông minh và phản ứng nhanh nhạy.

## 🚀 Tính Năng Mới

### 🧠 **Smart Random Movement (Di Chuyển Ngẫu Nhiên Thông Minh)**

- **Wandering**: Di chuyển đến điểm ngẫu nhiên trong vùng tròn
- **Idle**: Nghỉ ngơi ngắn giữa các lần di chuyển
- **Direction Change**: Thay đổi hướng ngẫu nhiên để khó đoán
- **Player Avoidance**: Tự động tránh player trên đường đi

### ⚡ **Ultra Evasion System (Hệ Thống Tránh Né Siêu Tốc)**

- **Player Detection**: Phát hiện player từ xa (3 units)
- **Approach Detection**: Phát hiện khi player đang tiến lại gần
- **Evasion Mode**: Chế độ tránh né cực nhanh (7 speed)
- **Flee Mode**: Chế độ chạy trốn thông thường (6 speed)

### 🎮 **Cài Đặt Trong Inspector**

#### **AI Settings:**

- **Detection Range**: 3 - Khoảng cách phát hiện player (giảm để phản ứng nhanh)
- **Use Player State Detection**: true - Detection theo trạng thái player

#### **Evasive Behavior:**

- **Evasion Speed**: 7 - Tốc độ tránh né cực nhanh
- **Evasion Duration**: 1 - Thời gian tránh né (giây)
- **Direction Change Chance**: 0.3 - Xác suất đổi hướng ngẫu nhiên

#### **Movement:**

- **Normal Speed**: 2.5 - Tốc độ di chuyển bình thường (tăng)
- **Flee Speed**: 6 - Tốc độ chạy trốn (tăng)
- **Circle Radius**: 5 - Bán kính vùng di chuyển ngẫu nhiên
- **Turn Speed**: 3 - Tốc độ quay (tăng)
- **Flee Distance**: 8 - Khoảng cách chạy trốn (tăng)

#### **Random Movement:**

- **Wander Speed**: 2 - Tốc độ di chuyển ngẫu nhiên
- **Wander Timer**: 2 - Thời gian di chuyển một hướng (giây)
- **Idle Timer**: 0.5 - Thời gian nghỉ ngắn (giây)
- **Enable Random Movement**: true - Bật di chuyển ngẫu nhiên

## 🔄 Cách Hoạt Động

### **4 Trạng Thái Thông Minh:**

1. **Wandering** (Di chuyển ngẫu nhiên):

   - Chọn điểm ngẫu nhiên trong vùng tròn
   - Di chuyển với tốc độ wanderSpeed
   - Thay đổi hướng ngẫu nhiên để khó đoán
   - Tự động tránh player trên đường đi

2. **Idle** (Nghỉ ngơi):

   - Dừng lại trong thời gian ngắn
   - Tạo nhịp điệu tự nhiên
   - Sau đó chuyển sang wandering

3. **Evasion** (Tránh né siêu tốc):

   - Khi phát hiện player đang tiến lại gần
   - Chạy với tốc độ cực nhanh (7)
   - Thay đổi hướng liên tục để khó bắt
   - Ưu tiên tránh player hơn obstacles

4. **Fleeing** (Chạy trốn):
   - Khi phát hiện player trong detection range
   - Chạy với tốc độ cao (6)
   - Chọn hướng tránh né ngẫu nhiên

### **Chuyển Đổi Trạng Thái:**

```
Wandering → Idle → Wandering → ... → Evasion/Fleeing (khi thấy player) → Wandering
```

## 🎨 Debug Gizmos

### **Trong Scene View:**

- **Vòng tròn vàng**: Detection range
- **Vòng tròn trắng**: Vùng di chuyển ngẫu nhiên
- **Chấm xám**: Vị trí bắt đầu
- **Chấm xanh dương**: Wander target (điểm đang di chuyển đến)
- **Chấm đỏ**: Evasion/Flee target
- **Đường xanh dương**: Hướng di chuyển khi wandering
- **Đường cam**: Hướng di chuyển khi evasion
- **Đường đỏ**: Hướng di chuyển khi fleeing

## ⚙️ Tùy Chỉnh Độ Khó

### **Để Làm Slime Khó Bắt Hơn:**

- Tăng **Evasion Speed** lên 8-10
- Tăng **Flee Speed** lên 7-8
- Giảm **Detection Range** xuống 2-2.5
- Tăng **Direction Change Chance** lên 0.4-0.5
- Giảm **Wander Timer** xuống 1-1.5

### **Để Làm Slime Dễ Bắt Hơn:**

- Giảm **Evasion Speed** xuống 5-6
- Giảm **Flee Speed** xuống 4-5
- Tăng **Detection Range** lên 4-5
- Giảm **Direction Change Chance** xuống 0.1-0.2
- Tăng **Wander Timer** lên 3-4

## 🎯 Kết Quả

Với những cải tiến này, slime sẽ:

- ✅ Di chuyển ngẫu nhiên trong vùng tròn thay vì vòng tròn cố định
- ✅ Phản ứng cực nhanh khi phát hiện player
- ✅ Tránh né thông minh với thay đổi hướng ngẫu nhiên
- ✅ Tốc độ cao và khó đoán
- ✅ **CỰC KỲ KHÓ BẮT** như yêu cầu!

## 🔧 Lưu Ý Kỹ Thuật

- Slime sẽ tự động tìm Player với tag "Player"
- Cần Rigidbody2D để hoạt động
- Obstacle avoidance vẫn hoạt động bình thường
- Tương thích với PlayerMovement script hiện có
