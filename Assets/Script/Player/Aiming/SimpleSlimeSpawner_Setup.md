# Hướng dẫn Setup SlimeSpawner đơn giản

## Tổng quan
Hệ thống spawn slime ngẫu nhiên cho adventure scene:
- Spawn 5-10 slime ngẫu nhiên trong bán kính 50 đơn vị
- Tự động spawn mới khi player di chuyển 100 đơn vị
- Xóa slime cũ khi spawn slime mới
- Tích hợp với SlimeAI hiện có

## Cách setup đơn giản

### Bước 1: Tạo Slime Prefab
1. Mở scene `adventureSence.unity`
2. Tìm GameObject "Slime" trong scene
3. Kéo thả vào folder `Assets/Prefab/` để tạo prefab
4. Đặt tên prefab là "SlimePrefab"

### Bước 2: Tạo SlimeSpawner
1. Tạo GameObject mới trong scene tên "SlimeSpawner"
2. Add component `SlimeSpawner`
3. Trong Inspector:
   - Assign slime prefab vào field "Slime Prefab"
   - Assign player vào field "Player" (hoặc để trống để tự động tìm)

### Bước 3: Chạy game
- Hệ thống sẽ tự động hoạt động
- Không có UI debug hiển thị trên màn hình
- Chỉ có log trong Console

## Cài đặt có thể chỉnh sửa

### Spawn Settings
- **Spawn Radius**: Bán kính spawn (mặc định: 50)
- **Min Slime Count**: Số slime tối thiểu (mặc định: 5)
- **Max Slime Count**: Số slime tối đa (mặc định: 10)
- **Movement Threshold**: Khoảng cách để spawn mới (mặc định: 100)

### Spawn Position Settings
- **Min Distance From Player**: Khoảng cách tối thiểu từ player (mặc định: 10)
- **Max Distance From Player**: Khoảng cách tối đa từ player (mặc định: 50)
- **Max Spawn Attempts**: Số lần thử spawn tối đa (mặc định: 50)

### Debug (Tùy chọn)
- **Show Debug Gizmos**: Hiển thị vòng tròn debug trong Scene view (mặc định: false)
- Các màu sắc và settings khác

## Cách hoạt động
1. Khi bắt đầu game, spawn 5-10 slime ngẫu nhiên
2. Theo dõi khoảng cách di chuyển của player
3. Khi player di chuyển 100 đơn vị, xóa slime cũ và spawn mới
4. Slime sử dụng SlimeAI để chạy trốn khi player đến gần

## Troubleshooting
- Nếu slime không spawn: Kiểm tra Console có lỗi gì không
- Nếu slime không di chuyển: Kiểm tra SlimeAI component
- Nếu muốn xem debug: Bật "Show Debug Gizmos" trong Inspector
