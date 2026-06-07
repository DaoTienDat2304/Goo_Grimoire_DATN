# Hướng dẫn SlimeAI thông minh - Sinh động và khó bắt

## Tổng quan
SlimeAI đã được làm lại hoàn toàn để slime thông minh, sinh động và khó bắt hơn.

## Tính năng chính

### 🧠 **Smart Movement (Di chuyển thông minh):**
- **Wandering**: Di chuyển đến điểm ngẫu nhiên
- **Idle**: Nghỉ ngơi giữa các lần di chuyển
- **Fleeing**: Chạy trốn khi thấy player
- **Avoidance**: Tránh player trên đường đi

### ⚙️ **Cài đặt trong Inspector:**

#### **Smart Movement:**
- **Move Speed**: 1.5 - Tốc độ di chuyển bình thường
- **Flee Speed**: 3 - Tốc độ chạy trốn (nhanh hơn)
- **Wander Radius**: 5 - Bán kính di chuyển ngẫu nhiên
- **Wander Speed**: 1.2 - Tốc độ di chuyển khi wandering
- **Idle Time**: 1 - Thời gian nghỉ (giây)
- **Wander Time**: 3 - Thời gian di chuyển một hướng (giây)
- **Enable Smart Movement**: true - Bật/tắt di chuyển thông minh

#### **AI Settings:**
- **Base Detection Range**: 4 - Khoảng cách phát hiện player
- **Use Player State Detection**: true - Detection theo trạng thái player

## Cách hoạt động

### 🔄 **3 trạng thái thông minh:**

1. **Wandering** (Di chuyển):
   - Chọn điểm ngẫu nhiên trong bán kính
   - Di chuyển đến điểm đó
   - Kiểm tra player trên đường đi
   - Thay đổi hướng ngẫu nhiên để sinh động

2. **Idle** (Nghỉ ngơi):
   - Dừng lại và nghỉ
   - Sau thời gian idleTime, chuyển sang wandering
   - Tạo nhịp điệu tự nhiên

3. **Fleeing** (Chạy trốn):
   - Khi thấy player trong detection range
   - Chạy với tốc độ fleeSpeed (nhanh hơn)
   - Ưu tiên tránh player hơn obstacles

### 🎯 **Tính năng thông minh:**

#### **Player Avoidance:**
- **IsPlayerInPath()**: Kiểm tra player có trên đường đi không
- **GetAvoidanceDirection()**: Chọn hướng vuông góc để tránh
- **Khoảng cách < 4**: Chỉ tránh khi player gần

#### **Random Direction Changes:**
- **30% chance** mỗi 2 giây
- **Thay đổi hướng ngẫu nhiên** để sinh động
- **Không dự đoán được** pattern di chuyển

#### **Smooth Movement:**
- **Vector3.Slerp**: Làm mượt hướng di chuyển
- **Không quay đột ngột**
- **Di chuyển tự nhiên**

## Cài đặt khuyến nghị

### **Cho slime dễ bắt:**
```
Move Speed: 1.0
Flee Speed: 2.0
Wander Radius: 3
Wander Speed: 0.8
Idle Time: 2
Wander Time: 2
Base Detection Range: 3
```

### **Cho slime khó bắt:**
```
Move Speed: 2.0
Flee Speed: 4.0
Wander Radius: 7
Wander Speed: 1.8
Idle Time: 0.5
Wander Time: 4
Base Detection Range: 6
```

### **Cho slime cân bằng:**
```
Move Speed: 1.5
Flee Speed: 3.0
Wander Radius: 5
Wander Speed: 1.2
Idle Time: 1
Wander Time: 3
Base Detection Range: 4
```

## Debug Gizmos

### **Trong Scene View:**
- **Vòng tròn trắng**: Wander radius (bán kính di chuyển)
- **Chấm xanh dương**: Wander target (điểm đang di chuyển đến)
- **Đường xanh**: Hướng di chuyển khi wandering
- **Đường đỏ**: Hướng chạy trốn khi fleeing
- **Chấm vàng**: Khi đang nghỉ (idle)
- **Vòng tròn màu**: Detection range

## Ưu điểm

### ✅ **Sinh động:**
- Thay đổi hướng ngẫu nhiên
- Nhịp điệu di chuyển tự nhiên
- Không có pattern cố định

### ✅ **Thông minh:**
- Tránh player trên đường đi
- Chọn hướng tránh thông minh
- Phản ứng nhanh với player

### ✅ **Khó bắt:**
- Di chuyển không dự đoán được
- Tốc độ chạy trốn nhanh
- Detection range linh hoạt

### ✅ **Tự nhiên:**
- Di chuyển mượt mà
- Có thời gian nghỉ ngơi
- Tránh obstacles hiệu quả

## Troubleshooting

### **Slime không di chuyển:**
- Kiểm tra **Enable Smart Movement** = true
- Kiểm tra **Wander Speed** > 0
- Kiểm tra **Wander Radius** > 0

### **Slime không tránh player:**
- Kiểm tra **Base Detection Range**
- Kiểm tra player có tag "Player" không
- Kiểm tra **Use Player State Detection**

### **Slime di chuyển quá nhanh/chậm:**
- Điều chỉnh **Wander Speed**
- Điều chỉnh **Flee Speed**
- Điều chỉnh **Move Speed**

### **Slime không sinh động:**
- Tăng **Wander Radius**
- Giảm **Idle Time**
- Tăng **Wander Time**

## Kết luận

Hệ thống SlimeAI thông minh mới cung cấp:
- Di chuyển sinh động và tự nhiên
- Tránh player thông minh
- Khó bắt và thú vị
- Cài đặt linh hoạt
- Performance tốt

Slime bây giờ sẽ thông minh, sinh động và khó bắt hơn nhiều!
