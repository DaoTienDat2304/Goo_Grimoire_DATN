# Hướng dẫn SlimeAI thông minh

## Tính năng mới

SlimeAI đã được cải thiện để slime thông minh hơn:

### 🧠 **Wandering Behavior** (Hành vi di chuyển tự do)

- **Di chuyển ngẫu nhiên**: Slime sẽ đi qua đi lại trong khu vực
- **Nghỉ ngơi**: Slime sẽ dừng lại nghỉ giữa các lần di chuyển
- **Tránh obstacles**: Tự động tránh vật cản khi di chuyển
- **Quay đầu chạy**: Khi thấy player, slime sẽ quay đầu chạy đi

### ⚙️ **Cài đặt mới trong Inspector**

#### **Wandering Behavior**

- **Wander Speed**: Tốc độ di chuyển bình thường (mặc định: 1)
- **Wander Radius**: Bán kính di chuyển ngẫu nhiên (mặc định: 3)
- **Wander Timer**: Thời gian di chuyển theo một hướng (mặc định: 2 giây)
- **Idle Timer**: Thời gian nghỉ giữa các lần di chuyển (mặc định: 1 giây)
- **Enable Wandering**: Bật/tắt chế độ di chuyển tự do (mặc định: true)

## Cách hoạt động

### 🎯 **3 trạng thái của Slime:**

1. **Wandering** (Di chuyển):

   - Slime chọn một điểm ngẫu nhiên trong bán kính
   - Di chuyển đến điểm đó với tốc độ wanderSpeed
   - Tránh obstacles trên đường đi

2. **Idle** (Nghỉ ngơi):

   - Slime dừng lại và nghỉ
   - Sau thời gian idleTimer, chuyển sang wandering

3. **Fleeing** (Chạy trốn):
   - Khi thấy player trong detection range
   - Chạy với tốc độ fleeSpeed (nhanh hơn wandering)
   - Ưu tiên tránh player hơn obstacles

### 🔄 **Chuyển đổi trạng thái:**

```
Wandering → Idle → Wandering → ... → Fleeing (khi thấy player) → Wandering
```

## Debug Gizmos

### **Trong Scene View:**

- **Vòng tròn trắng**: Wander radius (bán kính di chuyển)
- **Chấm xanh dương**: Wander target (điểm đang di chuyển đến)
- **Đường xanh**: Hướng di chuyển khi wandering
- **Đường đỏ**: Hướng chạy trốn khi fleeing
- **Vòng tròn màu**: Detection range (thay đổi theo trạng thái player)

## Tùy chỉnh

### **Slime nhanh nhẹn hơn:**

- Tăng **Wander Speed**: 1.5-2
- Giảm **Wander Timer**: 1-1.5
- Giảm **Idle Timer**: 0.5-0.8

### **Slime chậm rãi hơn:**

- Giảm **Wander Speed**: 0.5-0.8
- Tăng **Wander Timer**: 3-4
- Tăng **Idle Timer**: 2-3

### **Slime di chuyển xa hơn:**

- Tăng **Wander Radius**: 5-8

### **Slime di chuyển gần hơn:**

- Giảm **Wander Radius**: 1-2

## Troubleshooting

### **Slime không di chuyển:**

- Kiểm tra **Enable Wandering** = true
- Kiểm tra **Wander Speed** > 0
- Kiểm tra có obstacles chặn không

### **Slime di chuyển quá nhanh/chậm:**

- Điều chỉnh **Wander Speed**
- Điều chỉnh **Flee Speed**

### **Slime không chạy trốn:**

- Kiểm tra **Detection Range**
- Kiểm tra player có tag "Player" không
- Kiểm tra **Use Player State Detection**

## Ví dụ cài đặt

### **Slime thông minh:**

- Wander Speed: 1.2
- Wander Radius: 4
- Wander Timer: 2
- Idle Timer: 1
- Flee Speed: 2.5

### **Slime lười biếng:**

- Wander Speed: 0.6
- Wander Radius: 2
- Wander Timer: 4
- Idle Timer: 3
- Flee Speed: 1.8

### **Slime năng động:**

- Wander Speed: 1.8
- Wander Radius: 6
- Wander Timer: 1.5
- Idle Timer: 0.5
- Flee Speed: 3
