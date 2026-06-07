# SlimeAI CỰC KỲ KHÓ BẮT - Hướng Dẫn Siêu Khó

## 🎯 Tổng Quan

SlimeAI đã được nâng cấp lên mức **CỰC KỲ KHÓ BẮT** với tốc độ siêu nhanh, phản ứng tức thì, và chuyển động hỗn loạn không thể đoán trước!

## 🚀 Tính Năng Siêu Khó

### ⚡ **Ultra Fast Movement (Di Chuyển Siêu Nhanh)**

- **Wandering Speed**: 3.5 - Tốc độ di chuyển ngẫu nhiên cực nhanh
- **Flee Speed**: 10 - Tốc độ chạy trốn siêu nhanh
- **Evasion Speed**: 12 - Tốc độ tránh né cực nhanh
- **Chaos Speed**: 8 - Tốc độ hỗn loạn khi bị đuổi

### 🧠 **Instant Reaction (Phản Ứng Tức Thì)**

- **Detection Range**: 2 - Phát hiện player từ cực gần (phản ứng tức thì)
- **Direction Change**: 60% - Xác suất đổi hướng cực cao
- **Speed Variation**: 30% - Biến thiên tốc độ ngẫu nhiên
- **Angle Variation**: 45° - Biến thiên góc ngẫu nhiên

### 🌪️ **Chaotic Behavior (Hành Vi Hỗn Loạn)**

- **Chaos Mode**: Chế độ di chuyển hoàn toàn ngẫu nhiên
- **Chaos Chance**: 40% - Xác suất chuyển sang chế độ hỗn loạn
- **Continuous Direction Change**: Thay đổi hướng liên tục
- **Unpredictable Speed**: Tốc độ không thể đoán trước

## 🎮 **Cài Đặt Trong Inspector**

### **AI Settings:**

- **Detection Range**: 2 - Khoảng cách phát hiện player (cực ngắn)
- **Use Player State Detection**: true

### **Evasive Behavior:**

- **Evasion Speed**: 12 - Tốc độ tránh né siêu nhanh
- **Evasion Duration**: 0.8 - Thời gian tránh né ngắn
- **Direction Change Chance**: 0.6 - Xác suất đổi hướng cao

### **Movement:**

- **Normal Speed**: 4 - Tốc độ di chuyển bình thường (siêu nhanh)
- **Flee Speed**: 10 - Tốc độ chạy trốn (cực nhanh)
- **Circle Radius**: 6 - Bán kính vùng di chuyển (rộng hơn)
- **Turn Speed**: 5 - Tốc độ quay (siêu nhanh)
- **Flee Distance**: 12 - Khoảng cách chạy trốn (xa hơn)

### **Random Movement:**

- **Wander Speed**: 3.5 - Tốc độ di chuyển ngẫu nhiên (nhanh)
- **Wander Timer**: 1.2 - Thời gian di chuyển một hướng (ngắn)
- **Idle Timer**: 0.2 - Thời gian nghỉ cực ngắn
- **Enable Random Movement**: true

### **Chaotic Behavior:**

- **Chaos Speed**: 8 - Tốc độ hỗn loạn
- **Chaos Chance**: 0.4 - Xác suất chế độ hỗn loạn
- **Speed Variation**: 0.3 - Biến thiên tốc độ
- **Angle Variation**: 45 - Biến thiên góc

## 🔄 **5 Trạng Thái Siêu Khó**

### **1. Wandering (Di chuyển ngẫu nhiên)**

- Tốc độ: 3.5 + biến thiên 30%
- Thay đổi hướng: 60% mỗi frame
- Thời gian: 1.2 giây mỗi hướng

### **2. Idle (Nghỉ ngơi)**

- Thời gian: 0.2 giây (cực ngắn)
- Tạo nhịp điệu không đều

### **3. Evasion (Tránh né siêu tốc)**

- Tốc độ: 12 + biến thiên 15%
- Thay đổi hướng: 120% mỗi frame
- Thời gian: 0.8 giây

### **4. Fleeing (Chạy trốn)**

- Tốc độ: 10 + biến thiên
- Hướng: Ngẫu nhiên hoàn toàn
- Khoảng cách: 12 units

### **5. Chaotic (Hỗn loạn)**

- Tốc độ: 8 + biến thiên 60%
- Hướng: Thay đổi liên tục
- Không thể đoán trước

## 🎯 **Kết Quả Siêu Khó**

Với những cải tiến này, slime sẽ:

- ✅ **Tốc độ cực nhanh**: 3.5-12 speed
- ✅ **Phản ứng tức thì**: Detection range chỉ 2 units
- ✅ **Thay đổi hướng liên tục**: 60% mỗi frame
- ✅ **Biến thiên tốc độ**: 30-60% ngẫu nhiên
- ✅ **Chế độ hỗn loạn**: 40% cơ hội
- ✅ **CỰC KỲ KHÓ BẮT**: Gần như không thể bắt được!

## ⚙️ **Tùy Chỉnh Độ Khó**

### **Để Làm Cực Kỳ Khó Bắt:**

- Tăng **Evasion Speed** lên 15
- Tăng **Flee Speed** lên 12
- Giảm **Detection Range** xuống 1.5
- Tăng **Direction Change Chance** lên 0.8
- Tăng **Chaos Chance** lên 0.6

### **Để Làm Dễ Bắt Hơn:**

- Giảm **Evasion Speed** xuống 8
- Giảm **Flee Speed** xuống 6
- Tăng **Detection Range** lên 3
- Giảm **Direction Change Chance** xuống 0.3
- Giảm **Chaos Chance** xuống 0.2

## 🎨 **Debug Gizmos**

- **Vòng tròn vàng**: Detection range (2 units)
- **Vòng tròn trắng**: Vùng di chuyển (6 units)
- **Đường cam**: Evasion mode (tốc độ 12)
- **Đường đỏ**: Flee mode (tốc độ 10)
- **Đường tím**: Chaotic mode (tốc độ 8)
- **Đường xanh dương**: Wandering mode (tốc độ 3.5)

## ⚠️ **Cảnh Báo**

Slime này **CỰC KỲ KHÓ BẮT**! Có thể gây:

- 😵 Chóng mặt cho người chơi
- 🎯 Khó khăn cực độ
- 🏃‍♂️ Tốc độ siêu nhanh
- 🌪️ Chuyển động hỗn loạn

**Chỉ dành cho người chơi có kinh nghiệm!** 🎮
