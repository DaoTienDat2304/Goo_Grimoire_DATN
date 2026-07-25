# Thiết kế Thành tựu & Nhiệm vụ — Goo Grimoire

> Bản nháp để **duyệt trước khi code**. Mọi con số mốc / phần thưởng đều là *gợi ý, chỉnh được sau khi triển khai*.
> Nguyên tắc: tất cả mốc & thưởng nằm trong **ScriptableObject/config asset** → tune trong Unity Inspector, không sửa code.

## Quy ước phần thưởng (đã chốt)
- **Thành tựu → thưởng GEM** (mốc càng cao/hiếm, gem càng nhiều).
- **Nhiệm vụ (chính + hàng ngày) → thưởng VÀNG.**
- **"Vượt màn" = Tầng Tháp (Tower).** Dùng `towerHighestFloor` (counter bền vững duy nhất đang có).

---

## Ghi chú quan trọng về dữ liệu (đọc trước)

Hệ thống hiện tại **chưa có bộ đếm lifetime nào**. Tất cả đều là "trạng thái hiện tại":
- `BreedingManager.GetAllSlimes().Count` = số slime **đang sở hữu** (tối đa 30), KHÔNG phải tổng đã tạo.
- `CurrencyManager` reset về 5000/5000 mỗi lần Play; `ArchievementManager.resetAchievementsOnPlay = true`.
- Chỉ có `towerHighestFloor` là counter tiến trình bền vững duy nhất trong save.

➡️ Để làm được các thành tựu bên dưới, cần **thêm các biến tích luỹ (lifetime accumulators)** vào `GameSaveData` và tăng chúng tại các điểm móc (xem PHẦN 4).

### Enum độ hiếm thật trong game (8 bậc)
`Common → Uncommon → Rare → SuperRare → UltraRare → Legendary → Mythic → Secret`
(Tên đúng là `SuperRare`, `UltraRare` — dùng đúng enum, so sánh theo `(int)Rarity`.)

### Tiền tệ
`CurrencyType { Coins, Gems }` — Coins = vàng, Gems = gem.

---

## PHẦN 1 — THÀNH TỰU (Achievements) — thưởng **GEM**

Mỗi thành tựu là một **chuỗi bậc**. Cột "Counter mới?" = có phải thêm biến tích luỹ vào save không.

### A. Lai tạo — "Nhà lai tạo" — tổng slime đã lai tạo (lifetime)
| Bậc | Mốc | Thưởng | Nguồn | Counter mới? |
|----|-----|------|------|------|
| I   | 10    | 5 gem   | `totalSlimesBred` | ✅ tăng ở `CompleteBreeding()` |
| II  | 50    | 15 gem  | ↑ | |
| III | 100   | 40 gem  | ↑ | |
| IV  | 500   | 100 gem | ↑ | |
| V   | 1.000 | 300 gem + tiêu đề | ↑ | |

### B. Sưu tập Trait — "Nhà sưu tầm" — số trait KHÁC NHAU từng thu thập
Dùng ledger bền vững `unlockedTraitsEver` (đã-từng-thấy, không recompute mất).
| Bậc | Mốc | Thưởng | Nguồn |
|----|-----|------|------|
| I   | 10  | 10 gem | `unlockedTraitsEver.Count` |
| II  | 25  | 25 gem | ↑ |
| III | 50  | 60 gem | ↑ |
| IV  | 100 | 150 gem | ↑ |
| V   | Tất cả trait | 400 gem + tiêu đề | so với `SlimeGen.allTraits.Count` |

### C. Vàng kiếm được — "Trọc phú" — tổng vàng kiếm cộng dồn
| Bậc | Mốc | Thưởng | Nguồn | Counter mới? |
|----|-----|------|------|------|
| I   | 1.000     | 15 gem | `totalCoinsEarned` | ✅ sub `CurrencyManager.OnCurrencyAdded(Coins)` |
| II  | 10.000    | 40 gem | ↑ | |
| III | 100.000   | 120 gem | ↑ | |
| IV  | 1.000.000 | 500 gem + tiêu đề | ↑ | |

### D. Gem kiếm được — "Kho báu" — tổng gem kiếm cộng dồn
| Bậc | Mốc | Thưởng | Nguồn | Counter mới? |
|----|-----|------|------|------|
| I   | 50    | 10 gem | `totalGemsEarned` | ✅ sub `OnCurrencyAdded(Gems)` |
| II  | 500   | 50 gem | ↑ | |
| III | 5.000 | 200 gem + tiêu đề | ↑ | |

### E. Farm — "Nông dân" — số lần thắng Farm
| Bậc | Mốc | Thưởng | Nguồn | Counter mới? |
|----|-----|------|------|------|
| I   | 1   | 5 gem   | `totalFarmWins` | ✅ tăng ở `FarmModeManager.OnFarmVictory()` |
| II  | 10  | 25 gem  | ↑ | |
| III | 50  | 80 gem  | ↑ | |
| IV  | 100 | 200 gem | ↑ | |

### F. Phiêu lưu — Bắt slime — "Thợ săn"
Cả 2 đường: minigame thuần hoá + thắng trận Adventure.
| Bậc | Mốc | Thưởng | Nguồn | Counter mới? |
|----|-----|------|------|------|
| I   | 10  | 10 gem  | `totalCaptures` | ✅ `tamingManager` + `TurnSystem` |
| II  | 30  | 30 gem  | ↑ | |
| III | 100 | 90 gem  | ↑ | |
| IV  | 300 | 250 gem + tiêu đề | ↑ | |

### G. Sưu tầm theo độ hiếm — "Săn hàng hiếm"
Số slime **từng sở hữu** theo từng bậc (tích luỹ mỗi khi 1 slime bậc đó được thêm: lai/bắt/secret).
Cần bộ đếm lifetime theo `Rarity`: `rarityObtainedCount[Rarity]` — ✅ counter mới.
| Độ hiếm | Các mốc | Thưởng (gem) |
|------|------|------|
| **SuperRare** | 1 / 10 / 50 | 10 / 40 / 120 |
| **UltraRare** | 1 / 10 / 50 | 20 / 80 / 200 |
| **Legendary** | 1 / 10 / 25 | 50 / 150 / 350 |
| **Mythic**    | 1 / 5 / 10  | 100 / 300 / 700 |
| **Secret**    | 1 / 3 / tất cả secret | 150 / 400 / 1000 + tiêu đề |

### H. Leo tháp — "Kẻ leo tháp" — dùng `towerHighestFloor` (KHÔNG cần counter mới)
| Bậc | Mốc (tầng) | Thưởng |
|----|-----|------|
| I   | 5  | 20 gem |
| II  | 10 | 60 gem |
| III | 20 | 150 gem |
| IV  | 50 | 500 gem + tiêu đề |

### I. Chiến đấu — "Chiến binh" — tổng trận thắng (mọi chế độ)
| Bậc | Mốc | Thưởng | Nguồn | Counter mới? |
|----|-----|------|------|------|
| I   | 10  | 10 gem  | `totalBattleWins` | ✅ ở `RegisterBattleWin`/`HandleVictory` |
| II  | 50  | 40 gem  | ↑ | |
| III | 200 | 120 gem | ↑ | |
| IV  | 500 | 300 gem + tiêu đề | ↑ | |

### J. Bộ sưu tập hiện có — "Vườn slime" — `GetAllSlimes().Count` (KHÔNG cần counter mới)
| Bậc | Mốc | Thưởng |
|----|-----|------|
| I   | 10 | 15 gem |
| II  | 20 | 40 gem |
| III | 30 (đầy chuồng) | 100 gem |

### K. Đột biến — "Nhà giả kim" — slime đột biến khi lai (`eggStatQuality == "Mutation"`)
| Bậc | Mốc | Thưởng | Counter mới? |
|----|-----|------|------|
| I   | 1  | 20 gem | ✅ `totalMutations` ở `CompleteBreeding()` |
| II  | 10 | 80 gem | ↑ |
| III | 50 | 300 gem + tiêu đề | ↑ |

**Tổng ~14 chuỗi ≈ 55–60 thành tựu (đều thưởng gem).**

---

## PHẦN 2 — NHIỆM VỤ CHÍNH (Main Quest chain) — thưởng **VÀNG**

Dùng lại framework Quest có sẵn (`BreedingQuest / BattleQuest / TowerQuest / CollectQuest / TimeQuest`) với `questreq` mở khóa dây chuyền. "Vượt màn" = tầng Tháp.

| STT | Tên | Loại (class) | Điều kiện | Mở sau | Thưởng (vàng) |
|----|------|------|------|------|------|
| 1  | Bước đầu làm quen | BreedingQuest | Lai tạo 1 slime | — | 200 |
| 2  | Lứa đầu tiên | BreedingQuest | Lai tạo 5 slime | #1 | 500 |
| 3  | Chuồng nhỏ | CollectQuest | Sở hữu 10 slime | #2 | 800 |
| 4  | Ra trận | BattleQuest (Adventure) | Thắng 1 trận phiêu lưu | #3 | 700 |
| 5  | Thợ săn tập sự | (capture) | Bắt 1 slime hoang | #4 | 900 |
| 6  | Vượt màn 3 | TowerQuest | Đạt tầng 3 | #4 | 1.200 |
| 7  | Lai giống chỉ định | CollectQuest (`requiredTrait`) | Lai ra slime có trait [X] | #2 | 1.500 |
| 8  | Đàn lớn | BreedingQuest | Lai tạo 10 slime | #2 | 1.500 |
| 9  | Vượt màn 5 | TowerQuest | Đạt tầng 5 | #6 | 2.500 |
| 10 | Nông trại vàng | BattleQuest (Farm) | Thắng 5 trận Farm | #4 | 2.500 |
| 11 | Chạm hàng hiếm | CollectQuest (`minRarity=Rare`) | Sở hữu 1 slime Rare+ | #8 | 3.000 |
| 12 | Vượt màn 10 | TowerQuest | Đạt tầng 10 | #9 | 5.000 |
| 13 | Siêu phẩm | CollectQuest (`minRarity=UltraRare`) | Lai/sở hữu 1 slime UltraRare | #11 | 8.000 |
| 14 | Huyền thoại | CollectQuest (`minRarity=Legendary`) | Sở hữu 1 slime Legendary | #13 | 15.000 |
| 15 | ... (mở rộng sau) | | | | |

> Danh sách khởi điểm — thêm/bớt/đổi mốc thoải mái. Mỗi quest là 1 asset ScriptableObject → tune trong Inspector.

---

## PHẦN 3 — NHIỆM VỤ HÀNG NGÀY (Daily) — hệ thống MỚI — thưởng **VÀNG**

Chưa tồn tại → cần thêm. Cơ chế đơn giản, dễ hơn quest chính, reset mỗi ngày.

### Cơ chế
- Mỗi ngày random chọn **3 nhiệm vụ** từ *pool daily*.
- Reset lúc **00:00 giờ địa phương** (so `DateTime.Now.Date` với `lastDailyResetDate` lưu trong save).
- Mỗi nhiệm vụ: mục tiêu nhỏ → hoàn thành → claim → thưởng vàng.
- Hoàn thành cả 3 → **bonus streak** (thêm vàng).
- Lưu trong save: `lastDailyResetDate`, `todayDailyIDs[]`, tiến trình + trạng thái claim từng cái.

### Pool nhiệm vụ hàng ngày (mốc nhỏ, thưởng vàng)
| Tên | Điều kiện | Thưởng (vàng) |
|------|------|------|
| Lai một lứa | Lai tạo 1 slime | 150 |
| Ra sân | Thắng 1 trận (mọi chế độ) | 150 |
| Đi săn | Bắt 1 slime hoang | 200 |
| Cày vàng | Thắng 1 trận Farm | 200 |
| Kiếm cơm | Kiếm 500 vàng hôm nay | 300 |
| Leo một tầng | Vượt 1 tầng tháp | 250 |
| Săn hiếm | Lai/bắt 1 slime Rare+ | 400 |
| Chăm đàn | Sở hữu đủ 5 slime | 150 |
| **Bonus streak** | Hoàn thành cả 3 daily | +500 |

---

## PHẦN 4 — CHUẨN BỊ TRIỂN KHAI (guide cho lập trình)

### 4.1 Thêm biến tích luỹ vào `GameSaveData` (SaveGameData.cs)
```
long   totalSlimesBred;
int    totalFarmWins;
int    totalCaptures;
int    totalBattleWins;
int    totalMutations;
long   totalCoinsEarned;
long   totalGemsEarned;
int[]  rarityObtainedCount;      // theo (int)Rarity, 8 phần tử
List<string> unlockedTraitsEver; // ledger trait đã-từng-thấy (bền vững)
// Daily:
string lastDailyResetDate;       // "yyyy-MM-dd"
List<DailyMissionDTO> dailyMissions; // {id, progress, claimed}
// Achievement: nâng AchievementDTO thêm progress
//   AchievementDTO { string name; bool unlocked; long progress; }
```

### 4.2 Điểm móc (nơi tăng counter)
| Tín hiệu | Vị trí móc |
|------|------|
| Lai tạo xong 1 slime | `BreedingManager.CompleteBreeding()` — `BreedingManager.cs:359` |
| Slime secret nhận được | `BreedingManager.GenSpecialSlime()` — `:191` |
| Bắt slime (minigame) | `tamingManager.cs:101` |
| Bắt slime (sau trận) | `TurnSystem.cs:1208` |
| Thắng trận (mọi mode) | `TurnSystem.HandleVictory` → `RegisterBattleWin` — `TurnSystem.cs:1090` |
| Thắng Farm | `FarmModeManager.OnFarmVictory()` — `FarmModeManager.cs:304` |
| Vượt tầng tháp | `TowerSlimeBosses.AdvanceToNextFloor()` — `:136` (đã có `towerHighestFloor`) |
| Vàng/Gem kiếm được | event `CurrencyManager.OnCurrencyAdded` — `CurrencyManager.cs:17` |
| Độ hiếm slime mới | tính `SelectiveBreeding.GetSlimeRarity(slime)` tại các điểm add slime |

### 4.3 Cần TẮT trước khi test thật
- `ArchievementManager.resetAchievementsOnPlay` → **false**.
- `CurrencyManager.InitializeCurrencies()` đang hard-reset 5000/5000 mỗi Play → nối lại load-from-save (đang bị comment).

### 4.4 Kiến trúc đề xuất (để "hiệu chỉnh sau khi triển khai")
- **1 asset config cho mỗi thành tựu-bậc** (mở rộng `ArchievementPre`): thêm `enum AchievementMetric` (TotalBred, DistinctTraits, CoinsEarned, GemsEarned, FarmWins, Captures, RarityObtained + `Rarity target`, TowerFloor, BattleWins, OwnedSlimes, Mutations) + `long targetValue` + `gemReward`.
- **1 `AchievementService`** đọc counter từ save, so `targetValue`, set `unlocked`, trả thưởng gem, lưu. Data-driven → thêm/sửa mốc = tạo/sửa asset, không sửa code.
- Daily: `DailyMissionManager` + pool asset + logic reset theo ngày (thưởng vàng).

---

## Tóm tắt số lượng
- **Thành tựu:** ~14 chuỗi, ~55–60 mục — thưởng **gem**.
- **Nhiệm vụ chính:** ~14 (mở rộng dần) — thưởng **vàng**.
- **Daily:** pool ~8 + bonus streak, chọn 3/ngày — thưởng **vàng**.
- **Nền bắt buộc:** ~10 biến lifetime + ledger trait + hệ daily trong `GameSaveData`, cùng ~9 điểm móc.
