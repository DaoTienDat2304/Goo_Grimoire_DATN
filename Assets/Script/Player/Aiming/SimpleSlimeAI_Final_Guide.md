# Hướng dẫn SlimeAI đơn giản - Di chuyển vòng tròn

## Tổng quan

SlimeAI đã được đơn giản hóa để slime di chuyển trong vòng tròn và tránh player khi gặp.

## Tính năng chính

### 🎯 **Di chuyển vòng tròn:**

- Slime di chuyển quanh vòng tròn với bán kính cố định
- Tự động đổi hướng khi gặp player trên đường đi
- Chạy trốn khi thấy player trong detection range

### ⚙️ **Cài đặt trong Inspector:**

#### **Simple Circle Movement:**

- **Move Speed**: 1.5 - Tốc độ di chuyển
- **Circle Radius**: 4 - Bán kính vòng tròn di chuyển
- **Turn Speed**: 2 - Tốc độ quay quanh vòng tròn
- **Enable Circle Movement**: true - Bật/tắt di chuyển vòng tròn

#### **AI Settings:**

- **Base Detection Range**: 4 - Khoảng cách phát hiện player
- **Flee Speed**: 2 - Tốc độ chạy trốn
- **Use Player State Detection**: true - Detection theo trạng thái player

## Cách hoạt động

### 🔄 **3 trạng thái:**

1. **Circle Movement** (Di chuyển vòng tròn):

   - Slime di chuyển quanh vòng tròn
   - Kiểm tra player trên đường đi
   - Đổi hướng nếu gặp player

2. **Fleeing** (Chạy trốn):

   - Khi thấy player trong detection range
   - Chạy với tốc độ fleeSpeed
   - Tránh obstacles

3. **Idle** (Nghỉ):
   - Khi không di chuyển
   - Chờ chuyển sang trạng thái khác

### 🎮 **Logic tránh player:**

- **IsPlayerInPath()**: Kiểm tra player có trên đường đi không
- **Khoảng cách < 3**: Chỉ tránh khi player gần
- **Đổi hướng**: Quay ngược lại khi gặp player

## Cài đặt khuyến nghị

### **Cho slime dễ bắt:**

```
Move Speed: 1.0
Circle Radius: 3
Turn Speed: 1.5
Flee Speed: 1.8
Base Detection Range: 3
```

### **Cho slime khó bắt:**

```
Move Speed: 2.0
Circle Radius: 5
Turn Speed: 3.0
Flee Speed: 3.5
Base Detection Range: 6
```

### **Cho slime cân bằng:**

```
Move Speed: 1.5
Circle Radius: 4
Turn Speed: 2.0
Flee Speed: 2.5
Base Detection Range: 4
```

## Debug Gizmos

### **Trong Scene View:**

- **Vòng tròn trắng**: Vòng tròn di chuyển
- **Chấm xanh dương**: Vị trí hiện tại trên vòng tròn
- **Đường xanh**: Hướng di chuyển bình thường
- **Đường đỏ**: Hướng chạy trốn
- **Vòng tròn màu**: Detection range

## Ưu điểm

### ✅ **Đơn giản:**

- Logic dễ hiểu và chỉnh sửa
- Không phức tạp như hệ thống cũ
- Dễ debug và tùy chỉnh

### ✅ **Dự đoán được:**

- Player biết slime sẽ di chuyển quanh vòng tròn
- Có thể lập kế hoạch bắt slime
- Tạo gameplay thú vị

### ✅ **Tránh player thông minh:**

- Tự động đổi hướng khi gặp player
- Không lao thẳng vào player
- Phản ứng nhanh và tự nhiên

## Troubleshooting

### **Slime không di chuyển:**

- Kiểm tra **Enable Circle Movement** = true
- Kiểm tra **Move Speed** > 0
- Kiểm tra **Circle Radius** > 0

### **Slime không tránh player:**

- Kiểm tra **Base Detection Range**
- Kiểm tra player có tag "Player" không
- Kiểm tra **Use Player State Detection**

### **Slime di chuyển quá nhanh/chậm:**

- Điều chỉnh **Move Speed**
- Điều chỉnh **Turn Speed**

## Kết luận

Hệ thống SlimeAI đơn giản này cung cấp:

- Di chuyển tự nhiên trong vòng tròn
- Tránh player thông minh
- Cài đặt dễ dàng
- Performance tốt
- Gameplay cân bằng

Slime bây giờ sẽ di chuyển đơn giản, dự đoán được và thú vị để bắt!
