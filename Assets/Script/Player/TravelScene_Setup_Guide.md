# Hướng Dẫn Setup Travel Scene với Dialogue System

## Tổng Quan
Hệ thống này cho phép Player di chuyển đến các target và tự động kích hoạt dialogue tại đó. Bao gồm các component chính:

1. **MovePlayer** - Quản lý di chuyển Player
2. **DialogueSystem** - Hệ thống dialogue chính
3. **DialogueTrigger** - Kích hoạt dialogue tại các target
4. **TravelSceneManager** - Quản lý tổng thể scene
5. **StoryManager** - Quản lý story points (đã cập nhật)

## Bước 1: Setup UI cho Dialogue System

### Tạo Dialogue UI Canvas
1. Tạo Canvas mới: `Right-click > UI > Canvas`
2. Đặt tên: "DialogueCanvas"
3. Thiết lập Canvas Scaler: `UI Scale Mode = Scale With Screen Size`

### Tạo Dialogue Panel
1. Tạo Panel: `Right-click DialogueCanvas > UI > Panel`
2. Đặt tên: "DialoguePanel"
3. Thiết lập:
   - Width: 800, Height: 200
   - Position: Bottom center
   - Background: Semi-transparent (Alpha: 200)

### Tạo UI Elements trong DialoguePanel
1. **Speaker Portrait** (Image):
   - Size: 150x150
   - Position: Left side
   - Source Image: Speaker portrait sprite

2. **Speaker Name** (TextMeshPro):
   - Position: Top of dialogue text
   - Font Size: 18
   - Color: White

3. **Dialogue Text** (TextMeshPro):
   - Position: Center-right
   - Font Size: 16
   - Color: White
   - Alignment: Top-left

4. **Continue Button** (Button):
   - Position: Bottom-right
   - Text: "Continue" hoặc "Tiếp tục"

5. **Skip Button** (Button):
   - Position: Bottom-left
   - Text: "Skip" hoặc "Bỏ qua"

## Bước 2: Setup Scripts

### 1. Setup DialogueSystem
1. Tạo Empty GameObject: "DialogueManager"
2. Add component: `DialogueSystem`
3. Kéo thả các UI elements vào các field tương ứng:
   - Dialogue Panel → dialoguePanel
   - Dialogue Text → dialogueText
   - Speaker Name → speakerNameText
   - Speaker Portrait → speakerPortrait
   - Continue Button → continueButton
   - Skip Button → skipButton

### 2. Setup TravelSceneManager
1. Tạo Empty GameObject: "TravelManager"
2. Add component: `TravelSceneManager`
3. Thiết lập:
   - Player: Kéo Player GameObject
   - Dialogue System: Kéo DialogueManager
   - Player Start Position: Tạo Empty GameObject làm vị trí bắt đầu

### 3. Setup Target Points
1. Tạo Empty GameObjects cho các target points
2. Đặt tên: "Target1", "Target2", "Target3", etc.
3. Đặt vị trí mong muốn trên scene
4. Thêm vào TravelSceneManager → Target Points list

### 4. Setup Dialogue Triggers
1. Tại mỗi target point, add component: `DialogueTrigger`
2. Thiết lập:
   - Dialogue System: Kéo DialogueManager
   - Dialogue Sequence Name: Tên sequence dialogue
   - Trigger On Player Reach: ✓
   - Trigger Once: ✓

## Bước 3: Tạo Dialogue Data

### Tạo Dialogue Sequences
1. Trong DialogueSystem component, mở Dialogue Sequences
2. Thêm sequence mới:
   - Dialogue Name: "Welcome"
   - Lines: Thêm các dòng dialogue

### Ví dụ Dialogue Sequence:
```
Dialogue Name: "Welcome"
Lines:
1. Speaker: "NPC1", Text: "Chào mừng bạn đến với thế giới này!"
2. Speaker: "Player", Text: "Cảm ơn bạn!"
3. Speaker: "NPC1", Text: "Hãy khám phá và tận hưởng cuộc phiêu lưu!"
```

## Bước 4: Setup Player

### Cập nhật Player GameObject
1. Đảm bảo Player có component `MovePlayer`
2. Thiết lập:
   - Speed: 5 (hoặc giá trị phù hợp)
   - Target: Để trống (sẽ được set bởi script)

### Thêm Tag cho Player
1. Chọn Player GameObject
2. Tag: "Player" (nếu chưa có)

## Bước 5: Setup Colliders (Tùy chọn)

### Nếu muốn sử dụng Trigger Colliders
1. Tại mỗi target point, add component: `Box Collider`
2. Thiết lập:
   - Is Trigger: ✓
   - Size: Phù hợp với khu vực trigger

## Bước 6: Testing

### Kiểm tra hoạt động
1. Play scene
2. Nhấn Space để di chuyển Player đến target tiếp theo
3. Khi Player đến target, dialogue sẽ tự động kích hoạt
4. Sử dụng Continue/Skip để điều khiển dialogue

### Debug
- Kiểm tra Console để xem các log messages
- Đảm bảo tất cả references đã được gán đúng
- Kiểm tra Player có tag "Player" không

## Các Tính Năng Nâng Cao

### 1. Visual Feedback
- Thêm highlight objects tại các target
- Sử dụng DialogueTrigger → Highlight Object

### 2. Multiple Dialogue Sequences
- Tạo nhiều dialogue sequences khác nhau
- Sử dụng Dialogue Sequence Name để phân biệt

### 3. Conditional Dialogue
- Thêm logic điều kiện trong DialogueTrigger
- Kiểm tra trạng thái game trước khi kích hoạt dialogue

### 4. Audio Integration
- Thêm AudioSource cho dialogue
- Phát âm thanh khi dialogue bắt đầu/kết thúc

## Troubleshooting

### Player không di chuyển
- Kiểm tra MovePlayer component có được gán đúng không
- Kiểm tra speed > 0
- Kiểm tra target có được set không

### Dialogue không kích hoạt
- Kiểm tra DialogueTrigger có được gán DialogueSystem không
- Kiểm tra Dialogue Sequence Name có đúng không
- Kiểm tra Player có tag "Player" không

### UI không hiển thị
- Kiểm tra Canvas có active không
- Kiểm tra Dialogue Panel có được gán vào DialogueSystem không
- Kiểm tra UI elements có được gán đúng không

## Input Controls
- **Space**: Di chuyển đến target tiếp theo
- **Space** (trong dialogue): Tiếp tục dialogue
- **R**: Reset scene
- **Click**: Kích hoạt dialogue (nếu triggerOnClick = true)

