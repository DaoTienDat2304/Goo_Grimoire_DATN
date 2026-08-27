# Remote Config Keys — GooGrimoire (bộ v1, 46 key)

> Bộ key này **thay thế hoàn toàn** 47 key cũ (thiết kế cho phiên bản trước khi chuẩn hoá
> chỉ số). Xem [§5 Danh sách key cũ cần xoá](#5-danh-sách-key-cũ-cần-xoá-trên-console).

**Nguyên tắc:** Remote Config là **lớp phủ (override)**, không phải nguồn dữ liệu bắt buộc.
Thiếu key / offline / JSON hỏng → game rơi về bảng hardcode trong code (hoặc giá trị
Inspector, xem cột "Fallback"). Giá trị mặc định của bộ này được đặt **trùng khít** với
bảng hardcode, trừ **Farm** (đã tái cân bằng) — nên publish bộ mặc định sẽ không làm
thay đổi cân bằng ngoài Farm.

| File | Vai trò |
|---|---|
| `RemoteConfigManager.cs` | Init Firebase, fetch, đọc key thô (`GetFloat/GetInt/GetString/GetBool/GetJson`) |
| `RemoteConfigSchema.cs` | Tên key + DTO JSON + toàn bộ giá trị mặc định (`RemoteConfigKeys.BuildDefaults()`) |
| `RemoteBalance.cs` | Bảng cân bằng đã parse — điểm duy nhất code game hỏi Remote Config |
| `remote_config_defaults.json` | File **Import** thẳng vào Firebase Console |

> Bật Remote Config: thêm `FIREBASE_REMOTE_CONFIG` vào
> **Project Settings → Player → Scripting Define Symbols**.

---

## 1. Cài đặt lần đầu trên Firebase Console

1. Firebase Console → **Remote Config**.
2. Menu **⋮** (góc phải) → **Import** → chọn
   `Assets/Script/RemoteConfig/remote_config_defaults.json`.
3. Đổi `save_hmac_salt` thành một chuỗi bí mật thật (đừng để giá trị mặc định).
4. Điền `dev_account_email` nếu cần tài khoản dev.
5. **Publish changes**.
6. Xoá thủ công 47 key cũ ở [§5](#5-danh-sách-key-cũ-cần-xoá-trên-console).

Chạy game rồi tìm log `[RemoteConfig]` — dòng `[Meta] config_version=1` xác nhận đã ăn config,
dòng `[Dev] dev_account_email="..."` xác nhận key dev đã về tới máy.

> **Test trong Unity Editor:** `RemoteConfigManager.fetchRemoteConfigInEditor` phải bật, nếu không
> Editor chỉ dùng default trong code và mọi key chỉ tồn tại trên Console (`dev_account_email`, …)
> sẽ rỗng. `CloudSaveProvider.useFirestoreInEditor` vẫn để tắt — save trong Editor chỉ ghi local.

---

## 2. Bảng key

### Nhóm 0 — Vận hành (8 key)

| Key | Type | Default | Công dụng |
|---|---|---|---|
| `config_version` | number | `1` | Số hiệu bộ config. Tăng lên mỗi lần đổi cấu trúc để đối chiếu với log. |
| `maintenance_enabled` | boolean | `false` | Cờ bảo trì. **Hiện chỉ expose API + log** — chưa gắn UI chặn màn hình. |
| `maintenance_message` | string | `""` | Nội dung thông báo bảo trì. |
| `min_supported_version` | string | `""` | Version tối thiểu (vd `1.2.0`). Rỗng = tắt. Đọc qua `RemoteConfigManager.NeedsForceUpdate`. |
| `feature_flags` | JSON | `{"eggSystem":true,"tower":true,"dailyMissions":true,"shop":true,"adventureCapture":true}` | Bật/tắt từng hệ thống. Đọc qua `RemoteBalance.Flags`. **Chưa gắn vào luồng game** — sẵn sàng cho bước sau. |
| `active_shop_id` | string | `"default"` | Chọn database shop. `"summer"` cần gán `summerShopItemsDatabase` trong Inspector `ShopItemsSpawner`. |
| `save_hmac_salt` | string | `"GooGrimoire_HmacFallback_v1"` | **BÍ MẬT** — salt derive HMAC key cho save data. Bắt buộc đổi trên Console. |
| `dev_account_email` | string | `""` | Email tài khoản dev (`DevAccountInitializer.IsDevAccount`). Tài khoản khớp email này, **ngay lần đầu tạo save**, được 30 slime đủ 8 độ hiếm + 999999 vàng + 999999 gem. So khớp không phân biệt hoa/thường và bỏ khoảng trắng thừa. ⚠️ Có hiệu lực **cả trong bản release** — để tắt hoàn toàn thì xoá rỗng key này rồi Publish. |

### Nhóm 1 — Chỉ số & chiến đấu (12 key)

| Key | Type | Default | Công dụng |
|---|---|---|---|
| `stat_balance_table` | JSON | 8 dòng | Range chỉ số theo độ hiếm. Ghi đè `StatBalance.Get` → ảnh hưởng **lai tạo, nở trứng, enemy Adventure, hiển thị**. |
| `boss_scaling_table` | JSON | 8 dòng | Hệ số boss Adventure theo độ hiếm & từng chỉ số. Ghi đè `BossStatScaling.Get`. |
| `battle_crit_rate_cap` | number | `0.75` | Trần Crit Rate. Phần vượt quy đổi 1:1 sang Crit DMG. |
| `battle_crit_dmg_cap` | number | `2.5` | Trần Crit DMG. Phần vượt quy đổi sang ATK. |
| `battle_def_reduction_per_point` | number | `0.008` | 1 DEF = bao nhiêu % giảm sát thương. |
| `battle_max_def_reduction` | number | `0.8` | Trần giảm sát thương do DEF. |
| `battle_crit_overflow_to_atk` | number | `5` | 1% Crit DMG vượt trần = bao nhiêu ATK / Magic ATK. |
| `battle_poison_percent_hp` | number | `0.04` | Sát thương độc mỗi stack theo % Max HP. |
| `battle_poison_max_stacks` | number | `3` | Số stack độc tối đa. |
| `battle_energy_per_action` | number | `10` | Năng lượng hồi mỗi lần gây/nhận sát thương. |
| `battle_skill_power_mult` | number | `1.5` | Hệ số nhân vào `skill.power`. **Thay `rarity_skill_power_mult` cũ — lần này có đọc thật.** |
| `battle_legacy_boss_multiplier` | number | `3` | Hệ số phẳng cho enemy KHÔNG dùng rarity scaling. Thay `boss_stat_multiplier` cũ. |

**Cấu trúc `stat_balance_table`** — bọc trong `{"rows":[...]}`, mỗi dòng:
```json
{"rarity":"Common","hpMin":1000,"hpMax":2000,"atkMin":100,"atkMax":200,
 "magMin":200,"magMax":400,"defMin":400,"defMax":800,
 "spdMin":80,"spdMax":100,"critRate":0.05,"critDmg":1.30}
```
`rarity` hợp lệ: `Common` `Uncommon` `Rare` `SuperRare` `UltraRare` `Legendary` `Mythic` `Secret`.
Dòng nào thiếu → độ hiếm đó dùng bảng hardcode.

**Cấu trúc `boss_scaling_table`**: `{"rarity":"Common","hp":4.0,"atk":1.2,"magic":1.2,"def":1.3,"speed":1.00}`

### Nhóm 2 — Lai tạo (5 key)

| Key | Type | Default | Công dụng |
|---|---|---|---|
| `breeding_tier_table` | JSON | 8 dòng | Giá vàng + thời gian (phút) + tỷ lệ đột biến mỗi lứa, theo độ hiếm trứng. Ghi đè `SelectiveBreeding.GetTierCost` & `GetMutationRate`. |
| `breeding_quality_bands` | JSON | 4 dải | Dải chất lượng stat roll khi lai. |
| `breeding_gem_per_minute` | number | `0.8` | Gem cần để rút ngắn 1 phút thời gian lai còn lại. |
| `breeding_diff_rarity_bias` | number | `0.2` | Độ mạnh thiên lệch roll khi bố mẹ khác độ hiếm. |
| `breeding_max_slimes` | number | `30` | Giới hạn slime trong bộ sưu tập. Fallback = Inspector `BreedingManager.maxSlimes`. |

**`breeding_tier_table`**: `{"rarity":"Common","gold":200,"minutes":1,"mutation":0.35}`

**Dải chất lượng** (dùng chung cho `breeding_quality_bands`, `egg_quality_bands`,
`adventure_quality_bands`): `{"name":"Good","weight":55,"min":0.40,"max":0.60}`.
`weight` là trọng số tương đối — **không cần cộng đủ 100**, code tự chuẩn hoá.
`min`/`max` là khoảng roll 0..1 áp lên range chỉ số.

> Riêng `egg_quality_bands`: `name` phải khớp enum `SlimeEggSystem.StatQuality`
> (`Poor` `Normal` `Good` `Excellent` `Perfect` `GodRoll`) — sai tên thì rơi về `Normal`.

### Nhóm 3 — Trứng (8 key)

| Key | Type | Default | Fallback khi thiếu |
|---|---|---|---|
| `egg_check_interval_seconds` | number | `60` | Inspector `SlimeEggSystem` |
| `egg_chance` | number | `0.5` | Inspector |
| `egg_max_unhatched` | number | `3` | Inspector |
| `egg_required_slimes` | number | `2` | Inspector |
| `egg_incubation_seconds` | number | `600` | Inspector |
| `egg_seconds_per_gem` | number | `60` | Inspector |
| `egg_rarity_weights` | JSON | 45/35/14/5/1 | Bảng hardcode |
| `egg_quality_bands` | JSON | 6 dải | Bảng hardcode |

**`egg_rarity_weights`**: `{"rows":[{"rarity":"Common","weight":45}, ...]}` — trọng số tương đối.

### Nhóm 4 — Adventure (1 key)

| Key | Type | Default | Công dụng |
|---|---|---|---|
| `adventure_quality_bands` | JSON | 55/28/12/5, sàn 0.40 | Chất lượng stat roll của enemy Adventure (`AdventureStatRoll`). |

### Nhóm 5 — Farm (1 key)

`farm_difficulty_table` — **đã tái cân bằng theo thang chỉ số mới**
(mid-range `StatBalance` của độ hiếm tương ứng × hệ số `BossStatScaling`).
Bảng cũ để boss Easy 100 HP trong khi slime Common đã 1000–2000 HP.
Trường `evade` **đã bỏ hẳn** vì hệ evade không còn trong combat.

| key | ~tương đương boss | hp | atk | magic | def | speed | critRate/critDmg | coins | gems |
|---|---|---|---|---|---|---|---|---|---|
| `easy` | Common | 6 000 | 180 | 360 | 780 | 90 | 0.05 / 1.30 | 500 | 0 |
| `medium` | Uncommon | 11 000 | 325 | 650 | 1 480 | 105 | 0.06 / 1.35 | 1 200 | 0 |
| `hard` | Rare | 24 000 | 640 | 1 290 | 2 790 | 121 | 0.08 / 1.45 | 3 000 | 2 |
| `extreme` | SuperRare | 46 000 | 1 160 | 2 325 | 5 270 | 141 | 0.10 / 1.55 | 7 000 | 5 |
| `hell` | UltraRare | 87 000 | 2 125 | 4 250 | 9 500 | 162 | 0.13 / 1.70 | 15 000 | 10 |

Mỗi dòng: `{"key":"easy","name":"Dễ","hp":6000,"atk":180,"magic":360,"def":780,"speed":90,"critRate":0.05,"critDmg":1.30,"coins":500,"gems":0}`
`key` khớp theo thứ tự bậc độ khó; `name` ghi đè tên hiển thị.

### Nhóm 6 — Tháp (2 key)

| Key | Type | Default |
|---|---|---|
| `tower_growth` | JSON | xem dưới |
| `tower_star_thresholds` | JSON | `{"threeStarMaxTurns":50,"twoStarMaxTurns":80}` |

```json
{"baseHP":6000,"baseAttack":180,"baseMagicAttack":360,"baseDefense":780,"baseSpeed":90,
 "statGrowthPerFloor":1.12,"rewardCoinsBase":400,"rewardGrowthPerFloor":1.08,
 "gemEveryNFloors":5,"gemAmount":5,"applyToAuthoredFloors":false}
```

Chỉ số tầng N = `base × statGrowthPerFloor^(N-1)` (luỹ tiến, thay công thức cộng tuyến tính cũ).

> ⚠ `applyToAuthoredFloors`
> - `false` (mặc định): công thức **chỉ** áp cho tầng sinh thêm ngoài `TowerSlimeBosses.asset`.
>   Các tầng 1..N đã thiết kế tay giữ nguyên số cũ.
> - `true`: ghi đè chỉ số + thưởng của **mọi** tầng (traits/waves không đụng tới).
>   Dùng khi muốn kéo cả tháp về thang chỉ số mới — các tầng authored hiện vẫn ở thang cũ
>   (tầng 1 = 80 HP), lệch hẳn với slime Common 1 000–2 000 HP.

### Nhóm 7 — Thưởng & tiến trình (9 key)

| Key | Type | Default | Công dụng |
|---|---|---|---|
| `reward_mult_mission_gold` | number | `1` | Hệ số nhân thưởng vàng Nhiệm vụ chính. |
| `reward_mult_daily_gold` | number | `1` | Hệ số nhân thưởng vàng Nhiệm vụ hàng ngày. |
| `reward_mult_achievement_gem` | number | `1` | Hệ số nhân thưởng gem Thành tựu. |
| `reward_mult_farm_coins` | number | `1` | Hệ số nhân thưởng vàng Farm. |
| `reward_mult_tower` | number | `1` | Hệ số nhân thưởng vàng + gem Tháp. |
| `daily_count` | number | `3` | Số daily mỗi ngày. Đổi giá trị → ngày hiện tại được roll lại. |
| `daily_streak_bonus_gold` | number | `500` | Bonus khi xong cả bộ daily. |
| `starting_coins` | number | `5000` | Vàng khởi đầu. Fallback = Inspector `CurrencyManager`. |
| `starting_gems` | number | `5000` | Gem khởi đầu. Fallback = Inspector. |

> Hệ số thưởng dùng `RemoteBalance.ScaleReward` — luôn giữ tối thiểu 1 khi giá trị gốc > 0,
> nên đặt `0.01` cũng không làm thưởng về 0.

---

## 3. Thời điểm có hiệu lực

| Thay đổi key | Ảnh hưởng | Cần restart? |
|---|---|---|
| `stat_balance_table` | Slime **mới tạo** (lai/nở/enemy) từ lần fetch tiếp theo. Slime đã có giữ chỉ số đã lưu. | Không |
| `boss_scaling_table` | Trận Adventure tiếp theo | Không |
| `battle_*` | Ngay lập tức (đọc mỗi lần tính) | Không |
| `battle_skill_power_mult` | Slime được load/khởi tạo lại; `RecalculateAllSlimes()` chạy sau mỗi lần fetch | Không |
| `breeding_*` | Lứa lai tiếp theo (lứa đang chạy giữ giá/thời gian đã chốt) | Không |
| `egg_*` | Chu kỳ sinh/ấp tiếp theo | Không |
| `farm_difficulty_table` | Lần chọn độ khó tiếp theo | Không |
| `tower_growth` | Tầng sinh thêm tiếp theo; bật `applyToAuthoredFloors` → mọi tầng | Không |
| `reward_mult_*`, `daily_*` | Nhiệm vụ được nạp lại (đổi scene / sang ngày mới) | Không |
| `starting_coins/gems` | Chỉ lúc `CurrencyManager` khởi tạo | Có |
| `active_shop_id` | Lần mở shop tiếp theo | Không |

---

## 4. Test không cần Firebase

`RemoteConfigManager` có sẵn override in-memory. Trong Editor:

```csharp
var rc = RemoteConfigManager.Instance;
rc.SetJson(RemoteConfigKeys.FarmDifficultyTable,
    "{\"rows\":[{\"key\":\"easy\",\"name\":\"Dễ\",\"hp\":600,\"atk\":18,\"magic\":36," +
    "\"def\":78,\"speed\":90,\"critRate\":0.05,\"critDmg\":1.30,\"coins\":500,\"gems\":0}]}");
rc.SetFloat(RemoteConfigKeys.BattleCritRateCap, 0.9f);
rc.ReapplyBalance();               // BẮT BUỘC — nạp lại vào RemoteBalance
FarmModeManager.Instance?.RefreshDifficultyStats();
```

`rc.ClearOverrides()` để trả về mặc định. Lưu ý `SetFloat/SetInt/SetString/SetBool` **thắng cả**
giá trị từ server — tiện khi debug, nhớ gỡ trước khi build.

---

## 5. Danh sách key cũ cần xoá trên Console

47 key dưới đây **không còn được code đọc**. Xoá thủ công sau khi Import bộ mới
(Import không tự xoá key thừa).

<details>
<summary>Bấm để mở danh sách</summary>

```
active_shop_id            ← GIỮ LẠI (vẫn dùng, đã có trong bộ mới)
boss_stat_multiplier      → battle_legacy_boss_multiplier
breeding_cooldown_seconds → bỏ (cooldown theo tier lai tạo)
breeding_cost_coins       → breeding_tier_table (cột gold)
breeding_max_slimes       ← GIỮ LẠI (đã có trong bộ mới)
breeding_mutation_chance  → breeding_tier_table (cột mutation)
breeding_time_seconds     → breeding_tier_table (cột minutes)
crit_damage_multiplier    → stat_balance_table (cột critDmg) + battle_crit_dmg_cap
rarity_mult_common        → bỏ (chỉ số không còn nhân hệ số độ hiếm)
rarity_mult_uncommon      → bỏ
rarity_mult_rare          → bỏ
rarity_mult_super_rare    → bỏ
rarity_mult_ultra_rare    → bỏ
rarity_mult_legendary     → bỏ
rarity_mult_mythic        → bỏ
rarity_skill_power_mult   → battle_skill_power_mult
farm_easy_boss_hp         → farm_difficulty_table
farm_easy_boss_attack     → farm_difficulty_table
farm_easy_boss_defense    → farm_difficulty_table
farm_easy_boss_speed      → farm_difficulty_table
farm_easy_boss_evade      → bỏ (hệ evade đã bị thay bằng hệ crit)
farm_easy_reward_coins    → farm_difficulty_table
farm_medium_*  (6 key)    → farm_difficulty_table / bỏ evade
farm_hard_*    (6 key)    → farm_difficulty_table / bỏ evade
farm_extreme_* (6 key)    → farm_difficulty_table / bỏ evade
farm_hell_*    (6 key)    → farm_difficulty_table / bỏ evade
```

Tóm lại: xoá **tất cả** key bắt đầu bằng `farm_`, `rarity_`, và các key
`boss_stat_multiplier`, `crit_damage_multiplier`, `breeding_time_seconds`,
`breeding_cost_coins`, `breeding_cooldown_seconds`, `breeding_mutation_chance`.
**Giữ** `active_shop_id` và `breeding_max_slimes`.

</details>

---

## 6. Cách thêm key mới

1. Thêm tên key vào `RemoteConfigKeys` (`RemoteConfigSchema.cs`).
2. Thêm default vào `RemoteConfigKeys.BuildDefaults()`.
3. Nếu là bảng: thêm DTO `[Serializable]` (nhớ bọc `{"rows":[...]}` — `JsonUtility`
   không parse được mảng ở gốc) và parse trong `RemoteBalance.Apply`.
4. Nơi tiêu thụ hỏi `RemoteBalance` (bảng) hoặc `RemoteBalance.FloatOr/IntOr`
   (giá trị đơn có fallback Inspector) — **luôn giữ đường fallback hardcode**.
5. Thêm key vào `remote_config_defaults.json` rồi Import lại.
6. Cập nhật file này.
