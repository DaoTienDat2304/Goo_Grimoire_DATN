# Hướng dẫn setup IAP — bán gem bằng tiền thật qua Google Play

Nối 4 gói gem trong Shop vào Google Play Billing.
Phần code đã xong; phần còn lại là Play Console, và **Play ép đúng thứ tự** — không nhảy cóc được.

| | |
|---|---|
| Package name | `com.DA.GooGrimoire` |
| Unity IAP | 5.4.2 |
| Unity | 6000.0.47f1 |
| Loại sản phẩm | Consumable |

---

## Đã xong trong code — bạn không phải làm lại

- Thêm `com.unity.purchasing 5.4.2` vào `Packages/manifest.json`
- `Assets/Script/IAP/IAPManager.cs` — kết nối store, lấy giá, mua, phát thưởng, xử lý đơn treo
- `Assets/Script/Shop/ShopRewardGranter.cs` — một chỗ phát thưởng chung cho mua bằng tiền game / xem ads / IAP
- 4 gói gem trong `ShopItems.asset` và `ShopItems-Summer.asset` đã bật `isIAP`
- Ô giá tự thay `$4.99` cứng bằng giá thật theo nội tệ người chơi

> ⚠️ **Làm ngay trước khi đọc tiếp**
> Click vào cửa sổ Unity để nó tải package IAP về. Chưa tải xong thì project sẽ báo lỗi
> biên dịch ở `IAPManager.cs` — đó là bình thường, không phải lỗi code.

### Về tên menu trong tài liệu này

Play Console hiển thị theo ngôn ngữ tài khoản của bạn, nên bên dưới tôi ghi **song ngữ**:
tiếng Việt trước, tiếng Anh trong ngoặc. Ví dụ **Kiếm tiền bằng Play** (*Monetise with Play*).

Google thỉnh thoảng đổi tên mục, và bản dịch tiếng Việt có thể lệch chút giữa các phiên bản
console. Nếu không thấy đúng tên, cứ dò theo tên tiếng Anh trong ngoặc — hoặc đổi giao diện
sang tiếng Anh ở góc trên bên phải, biểu tượng bánh răng → mục ngôn ngữ.

Riêng phần Unity thì giữ nguyên tiếng Anh vì Unity Editor không có bản tiếng Việt.

---

## 01 — Tạo app và hồ sơ thanh toán
*Play Console · làm một lần*

### Tạo app

Vào [Play Console](https://play.google.com/console) → **Tạo ứng dụng** (*Create app*).

Điền form:

| Trường | Tiếng Anh | Chọn gì |
|---|---|---|
| Tên ứng dụng | *App name* | Goo Grimoire |
| Ngôn ngữ mặc định | *Default language* | tuỳ bạn |
| Ứng dụng hoặc trò chơi | *App or game* | **Trò chơi** (*Game*) |
| Miễn phí hoặc có tính phí | *Free or paid* | **Miễn phí** (*Free*) |

> ⚠️ **Không đảo ngược được**
> App đã publish ở dạng Free thì **không đổi sang Paid được nữa**. Gem bán qua
> in-app product nên Free là đúng — cứ chọn Free.

### Thiết lập hồ sơ thanh toán (merchant account)

Đây là chỗ hay kẹt nhất. **Chưa có merchant account thì Play Console không cho tạo
in-app product** — mục Sản phẩm sẽ bị khoá.

Trong Play Console vào **Kiếm tiền bằng Play → Hồ sơ thanh toán**
(*Monetise with Play → Payments profile*) rồi làm theo hướng dẫn.
Google cần tên, địa chỉ, và thông tin thuế. Duyệt có thể mất **vài ngày làm việc**,
nên làm sớm nhất có thể. Đây là đường găng của cả quy trình.

---

## 02 — Bật IAP trong Unity và build AAB
*Unity Editor*

### Bật dịch vụ In-App Purchasing

Sau khi package tải xong, vào **Services → In-App Purchasing → Configure** và bật cho Android.
Bước này khiến Unity nhúng thư viện Google Play Billing vào build — thiếu nó thì app chạy
nhưng không kết nối được cửa hàng.

### Build Android App Bundle

**File → Build Settings → Android**, tick **Build App Bundle (Google Play)**, rồi Build.
Cấu hình hiện tại của project đã hợp lệ: minSdk 26, targetSdk 35, IL2CPP, ARM64.

Lần build đầu Unity sẽ hỏi keystore. Tạo mới, đặt mật khẩu, và **giữ kỹ file keystore đó** —
mất là không cập nhật app được nữa.

> ⚠️ **Nhớ tăng version**
> Mỗi lần upload lên Play, `Bundle Version Code` trong Player Settings phải **lớn hơn lần trước**.
> Quên là Play từ chối file.

---

## 03 — Upload bản build lên track thử nghiệm
*Play Console · phải làm trước khi tạo sản phẩm*

### Vì sao phải upload trước

Play lấy **package name** từ file AAB đầu tiên bạn upload, và chỉ mở phần in-app products
sau khi app đã có ít nhất một bản build trên một track. Không có bước này thì mọi thứ
phía sau đều bị khoá.

### Internal testing

Vào **Kiểm thử và phát hành → Kiểm thử → Kiểm thử nội bộ → Tạo bản phát hành mới**
(*Test and release → Testing → Internal testing → Create new release*), upload file `.aab`,
điền ghi chú phát hành (*release notes*), rồi **Xem xét bản phát hành** (*Review release*)
→ **Bắt đầu triển khai** (*Start rollout*).

Sang tab **Người kiểm thử** (*Testers*), tạo một danh sách email và thêm Gmail của bạn
(và của thầy cô nếu cần demo). Copy **đường liên kết chấp nhận tham gia**
(*opt-in link*) ở cuối trang — lát nữa cần đến.

Internal testing duyệt gần như tức thì, không phải chờ review như bản public.

---

## 04 — Tạo 4 in-app product
*Play Console · ID phải khớp tuyệt đối với code*

### Tạo sản phẩm

Vào **Kiếm tiền bằng Play → Sản phẩm → Sản phẩm trong ứng dụng → Tạo sản phẩm**
(*Monetise with Play → Products → In-app products → Create product*).

Tạo đúng 4 sản phẩm dưới đây. Ô **Mã sản phẩm** (*Product ID*) phải gõ
**chính xác từng ký tự** — code đang tìm theo đúng chuỗi này.

| Product ID | Tên gợi ý | Gem | Giá tham chiếu |
|---|---|---|---|
| `gems_5` | 5 Gems | 5 | $4.99 |
| `gems_12` | 12 Gems | 12 | $9.99 |
| `gems_30` | 30 Gems | 30 | $19.99 |
| `gems_80` | 80 Gems | 80 | $49.99 |

Giá là do bạn đặt, cột trên chỉ là mức đang ghi trong asset. Đặt giá VND cũng được —
code đọc giá thật từ Play nên ô giá trong Shop sẽ tự hiển thị đúng nội tệ của người chơi.

### Hai cái bẫy ở bước này

> ⚠️ **Mã sản phẩm là vĩnh viễn**
> Tạo xong **không sửa và không xoá để dùng lại được**. Gõ sai một ký tự là phải bỏ ID đó
> và tạo ID khác. Kiểm tra kỹ trước khi bấm lưu.

> ⚠️ **Phải bấm Kích hoạt**
> Sản phẩm mới tạo mặc định ở trạng thái **Không hoạt động** (*Inactive*) và sẽ không mua được.
> Vào từng sản phẩm bấm **Kích hoạt** (*Activate*). Đây là lý do phổ biến nhất khiến app báo
> "sản phẩm không tồn tại".

---

## 05 — Test không mất tiền
*License testing · bắt buộc test trên máy thật*

### Thêm license tester

Ở **trang chủ Play Console** (cấp tài khoản, không phải bên trong app) vào
**Thiết lập → Kiểm thử giấy phép** (*Setup → License testing*), thêm các Gmail sẽ dùng để test,
và để **Phản hồi giấy phép** (*License response*) = `RESPOND_NORMALLY`.

Các tài khoản này mua hàng sẽ hiện luồng thanh toán đầy đủ nhưng **không bị trừ tiền thật**.
Đây là cách demo đồ án an toàn.

### Cài app đúng cách

Mở **đường liên kết chấp nhận tham gia** (*opt-in link*) từ bước 03 trên điện thoại,
đăng nhập bằng đúng Gmail đã thêm ở trên, bấm tham gia rồi cài app **từ Google Play**.

> ⚠️ **Đừng cài bằng adb**
> Bản cài trực tiếp từ file APK/AAB sẽ không kết nối được billing và luôn báo sản phẩm
> không khả dụng. Billing chỉ hoạt động khi app được cài qua Play.

### Không test được trong Unity Editor

Đã kiểm tra source của IAP 5.4.2: package này **không còn FakeStore** như bản 4.x,
chỉ hỗ trợ apple, google, xbox và macos. Nghĩa là bấm mua trong Play mode sẽ không ra gì —
phải test trên máy Android thật.

Nếu muốn kiểm tra riêng phần cộng gem mà chưa dựng được Play Console, tạm sửa
`isIAP: 1` → `isIAP: 0` ở một gói trong `ShopItems.asset` — nó sẽ quay về cộng gem
miễn phí như cũ để bạn xác nhận logic phát thưởng.

---

## Xong rồi thì kiểm lại 4 điểm này

1. Mở Shop, ô giá 4 gói gem hiển thị **giá thật theo nội tệ**, không còn `$4.99`.
   Còn hiện `$4.99` nghĩa là chưa kết nối được store.
2. Bấm mua → hiện bảng thanh toán của Google Play có dòng chữ báo đây là giao dịch thử.
3. Mua xong, số gem tăng đúng và **giữ nguyên sau khi tắt mở lại game**.
4. Mua lại được gói vừa mua — vì gem là consumable, không phải mua một lần.

---

## Nên làm thêm khi nộp đồ án

Hiện code tin tưởng kết quả từ Play mà chưa kiểm chữ ký hoá đơn. Muốn chắc hơn thì thêm
local receipt validation — package có sẵn sample *Google Play Store · 06 Local Receipt Validation*,
cần lấy license key trong Play Console. Không bắt buộc cho bản demo.

## Bảng tra nhanh tên menu Play Console

| Tiếng Việt | Tiếng Anh |
|---|---|
| Tạo ứng dụng | Create app |
| Trò chơi / Miễn phí | Game / Free |
| Kiếm tiền bằng Play | Monetise with Play |
| Hồ sơ thanh toán | Payments profile |
| Sản phẩm trong ứng dụng | In-app products |
| Tạo sản phẩm | Create product |
| Mã sản phẩm | Product ID |
| Kích hoạt / Không hoạt động | Activate / Inactive |
| Kiểm thử và phát hành | Test and release |
| Kiểm thử nội bộ | Internal testing |
| Tạo bản phát hành mới | Create new release |
| Xem xét bản phát hành | Review release |
| Bắt đầu triển khai | Start rollout |
| Người kiểm thử | Testers |
| Đường liên kết chấp nhận tham gia | Opt-in link |
| Thiết lập | Setup |
| Kiểm thử giấy phép | License testing |
| Phản hồi giấy phép | License response |

## Liên quan

- Rewarded ads (AdMob) đang dùng key test — xem `Assets/Script/Ads/RewardedAdsManager.cs`,
  phải đổi sang ad unit thật trước khi phát hành.
