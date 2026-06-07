# Remote Config Keys — GooGrimoire

Tài liệu này liệt kê toàn bộ keys Firebase Remote Config, giá trị mặc định, kiểu dữ liệu, và công dụng của từng key.

> Để bật Remote Config: thêm `FIREBASE_REMOTE_CONFIG` vào **Project Settings → Player → Scripting Define Symbols**

---

## Breeding (Nhân giống)

| Key | Type | Default | Công dụng |
|---|---|---|---|
| `breeding_time_seconds` | float | `5.0` | Thời gian chờ để hoàn thành 1 lần nhân giống (giây). Giảm để event nhanh hơn. |
| `breeding_cost_coins` | int | `1` | Số Coins tiêu mỗi lần nhân giống. Tăng khi muốn làm khó economy. |
| `breeding_max_slimes` | int | `30` | Giới hạn tối đa slime trong bộ sưu tập. Tăng để mở rộng sau khi có building. |
| `breeding_mutation_chance` | float | `0.1` | Xác suất con sinh ra bị mutation (0.0 → 1.0). `0.1` = 10%. |
| `breeding_cooldown_seconds` | float | `2.0` | Thời gian cooldown của slime bố/mẹ sau khi nhân giống xong (giây). |

---

## Rarity Multipliers (Hệ số độ hiếm)

Áp dụng lên stats của `TraitInstance` khi slime được tạo ra. Thay đổi key này sẽ **recalculate toàn bộ slime hiện có** (kể cả slime trong save file) sau khi fetch xong.

| Key | Type | Default | Rarity áp dụng |
|---|---|---|---|
| `rarity_mult_common` | float | `1.0` | Common |
| `rarity_mult_uncommon` | float | `1.2` | Uncommon |
| `rarity_mult_rare` | float | `1.4` | Rare |
| `rarity_mult_super_rare` | float | `1.6` | Super Rare |
| `rarity_mult_ultra_rare` | float | `1.8` | Ultra Rare |
| `rarity_mult_legendary` | float | `2.0` | Legendary & Secret |
| `rarity_mult_mythic` | float | `2.25` | Mythic |
| `rarity_skill_power_mult` | float | `1.5` | Hệ số nhân thêm vào `skill.power` theo rarity (skill power = multiplier × giá trị này) |

> **Lưu ý:** Thay đổi rarity multiplier có hiệu lực **ngay lập tức** với tất cả slime vì `baseAttack` (giá trị gốc trước nhân) được lưu riêng trong save file.

**Công thức:** `finalAttack = baseAttack × rarity_mult_<rarity>`

---

## Battle (Chiến đấu)

| Key | Type | Default | Công dụng |
|---|---|---|---|
| `boss_stat_multiplier` | float | `3.0` | Boss mạnh hơn player bao nhiêu lần. Áp dụng cho HP, Attack, Defense, Speed của enemy. |
| `crit_damage_multiplier` | float | `1.5` | Damage nhân thêm khi critical hit. `1.5` = 150% damage. |

> **Lưu ý:** `boss_stat_multiplier` KHÔNG áp dụng cho Farm boss vì Farm boss có stats cố định. Chỉ áp dụng cho Tower boss và Adventure boss.

---

## Farm Mode (Chế độ Farm)

Stats cố định của boss theo từng độ khó. Thay đổi không ảnh hưởng trận đang diễn ra, chỉ có hiệu lực từ lần chọn độ khó tiếp theo.

> Pattern key: `farm_{difficulty}_boss_{stat}` và `farm_{difficulty}_reward_coins`
> Difficulty keys: `easy` | `medium` | `hard` | `extreme` | `hell`

### Dễ (`easy`)
| Key | Type | Default |
|---|---|---|
| `farm_easy_boss_hp` | int | `100` |
| `farm_easy_boss_attack` | int | `30` |
| `farm_easy_boss_defense` | int | `20` |
| `farm_easy_boss_speed` | int | `15` |
| `farm_easy_boss_evade` | int | `5` |
| `farm_easy_reward_coins` | int | `50` |

### Trung Bình (`medium`)
| Key | Type | Default |
|---|---|---|
| `farm_medium_boss_hp` | int | `200` |
| `farm_medium_boss_attack` | int | `60` |
| `farm_medium_boss_defense` | int | `40` |
| `farm_medium_boss_speed` | int | `25` |
| `farm_medium_boss_evade` | int | `10` |
| `farm_medium_reward_coins` | int | `150` |

### Khó (`hard`)
| Key | Type | Default |
|---|---|---|
| `farm_hard_boss_hp` | int | `400` |
| `farm_hard_boss_attack` | int | `120` |
| `farm_hard_boss_defense` | int | `80` |
| `farm_hard_boss_speed` | int | `40` |
| `farm_hard_boss_evade` | int | `20` |
| `farm_hard_reward_coins` | int | `300` |

### Cực Khó (`extreme`)
| Key | Type | Default |
|---|---|---|
| `farm_extreme_boss_hp` | int | `800` |
| `farm_extreme_boss_attack` | int | `200` |
| `farm_extreme_boss_defense` | int | `150` |
| `farm_extreme_boss_speed` | int | `60` |
| `farm_extreme_boss_evade` | int | `20` |
| `farm_extreme_reward_coins` | int | `600` |

### Địa Ngục (`hell`)
| Key | Type | Default |
|---|---|---|
| `farm_hell_boss_hp` | int | `1500` |
| `farm_hell_boss_attack` | int | `350` |
| `farm_hell_boss_defense` | int | `250` |
| `farm_hell_boss_speed` | int | `90` |
| `farm_hell_boss_evade` | int | `20` |
| `farm_hell_reward_coins` | int | `1200` |

---

## Shop

| Key | Type | Default | Công dụng |
|---|---|---|---|
| `active_shop_id` | string | `"default"` | Chọn database shop đang hiển thị. `"default"` = shop thường. `"summer"` = shop mùa hè (cần gán `summerShopItemsDatabase` trong Inspector của `ShopItemsSpawner`). |

> Thêm shop mới: tạo `ShopItems` ScriptableObject mới, gán vào `ShopItemsSpawner`, đặt tên ID, rồi set key này từ Firebase Console.

---

## Cách thêm key mới

1. Thêm property vào `RemoteConfigManager.cs`:
   ```csharp
   public int NewValue => GetInt("new_key_name", defaultValue);
   ```
2. Thêm default vào `SetDefaults()` trong cùng file:
   ```csharp
   { "new_key_name", defaultValue },
   ```
3. Tạo key tương ứng trên Firebase Console với cùng tên.
4. Cập nhật file này.

---

## Tóm tắt ảnh hưởng theo đối tượng

| Thay đổi key | Ảnh hưởng ngay | Cần restart game |
|---|---|---|
| `breeding_*` | Lần breeding tiếp theo | Không |
| `rarity_mult_*` | Tất cả slime hiện có (recalculate sau fetch) | Không |
| `boss_stat_multiplier` | Trận chiến tiếp theo | Không |
| `crit_damage_multiplier` | Lần đánh tiếp theo trong trận | Không |
| `farm_*` | Lần chọn difficulty tiếp theo | Không |
| `active_shop_id` | Lần mở shop tiếp theo | Không |
