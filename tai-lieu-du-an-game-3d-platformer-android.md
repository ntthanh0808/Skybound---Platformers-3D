# Tài Liệu Dự Án: Game 3D Platformer cho Android
### (Lấy cảm hứng từ Vista World – Steam)

---

## 1. Bối cảnh tham khảo: Vista World

Vista World là game 3D platformer đang phát triển trên Steam bởi studio indie **Platform Placer**, với các đặc điểm nổi bật:

- Thể loại: 3D Platformer / Collectathon / Sandbox / Third-person controller
- Gameplay lõi: nhảy, trượt (slide), lướt (dash) mượt mà, "cảm giác điều khiển" (game feel) là trọng tâm
- **Level Editor tích hợp**: người chơi tự xây dựng level, đặt bẫy, kẻ địch, chướng ngại vật — không cần biết lập trình
- **Chia sẻ cộng đồng**: chia sẻ level tự tạo, chơi level của người khác (giống Steam Workshop)
- **Hệ thống ability đa dạng**: ví dụ "rau củ thao túng trọng lực", "bong bóng lượn" (hang glider) — mỗi ability mở ra cách tương tác mới với môi trường
- Kẻ địch và bẫy được thiết kế để mỗi level có cảm giác khác nhau

> Lưu ý bản quyền: Tài liệu này chỉ dùng Vista World làm **nguồn cảm hứng về thể loại và cơ chế** (design pattern chung của thể loại collectathon platformer, vốn đã có từ Mario 64, Banjo-Kazooie, Spyro...). Không sao chép asset, cốt truyện, tên riêng, nhân vật, hay mã nguồn của Vista World.

---

## 2. Tầm nhìn sản phẩm (Product Vision)

**Ý tưởng cô đọng:** Một game 3D platformer chơi bằng cảm ứng trên Android, tập trung vào chuyển động mượt (di chuyển – nhảy – trượt – lướt), có hệ thống thu thập, và một Level Editor đơn giản hoá cho màn hình cảm ứng để người chơi tự tạo và chia sẻ level.

**Đối tượng người chơi:** người thích thể loại collectathon 3D (Mario, Banjo-Kazooie...) nhưng chơi trên di động; cộng đồng thích sáng tạo nội dung (UGC).

**Điểm khác biệt cần cân nhắc so với bản Steam:**
- Điều khiển chạm (virtual joystick + nút) thay vì bàn phím/gamepad → cần thiết kế lại camera và input
- Thiết bị di động giới hạn hiệu năng, pin, nhiệt độ → cần tối ưu nặng
- Level Editor trên màn hình nhỏ khó hơn nhiều so với PC → cần UI/UX riêng cho mobile

---

## 3. Các trụ cột gameplay (Core Pillars)

| Trụ cột | Mô tả |
|---|---|
| **Movement Feel** | Nhân vật di chuyển "nặng tay, phản hồi tốt": nhảy có buffer/coyote time, trượt có quán tính, dash có hiệu ứng rõ ràng |
| **Collectathon Loop** | Mỗi level có vật phẩm thu thập (sao, đồng xu, mảnh ghép) thúc đẩy khám phá |
| **Ability Variety** | Vài khả năng đặc biệt (glide, double jump, ground-pound...) mở khoá theo tiến trình |
| **Creation & Sharing** | Level Editor rút gọn cho mobile + hệ thống upload/tải level cộng đồng |
| **Bite-sized Sessions** | Level ngắn (2–8 phút) phù hợp thói quen chơi mobile |

---

## 4. Lộ trình tổng thể theo giai đoạn

```
Giai đoạn 0: Tiền sản xuất (2-4 tuần)
Giai đoạn 1: Prototype (4-6 tuần)
Giai đoạn 2: Vertical Slice (6-10 tuần)
Giai đoạn 3: Production (3-6 tháng)
Giai đoạn 4: Polish & Optimize (4-8 tuần)
Giai đoạn 5: Soft Launch & Launch (liên tục)
```

---

## 5. Chi tiết từng giai đoạn

### Giai đoạn 0 — Tiền sản xuất

1. **Xác định phạm vi (scope)**: số lượng world, số level ban đầu (khuyến nghị 10–15 level cho bản đầu), số ability
2. **Chọn công cụ (Engine)**:
   - **Unity** (khuyến nghị): hỗ trợ Android tốt nhất, nhiều asset/plugin 3D, cộng đồng lớn, dễ tuyển nhân sự
   - **Unreal Engine**: đồ hoạ đẹp hơn nhưng nặng máy hơn cho mobile, learning curve cao hơn
   - **Godot 4**: miễn phí, nhẹ, C#/GDScript, phù hợp team nhỏ/indie ngân sách thấp, nhưng ecosystem mobile còn non hơn Unity
   - → Với game platformer 3D + UGC editor, **Unity (URP - Universal Render Pipeline)** là lựa chọn an toàn nhất về hiệu năng mobile và tài liệu
3. **Thiết kế tài liệu GDD (Game Design Document)**: cơ chế di chuyển, danh sách ability, cấu trúc level, hệ thống điểm/thu thập
4. **Phác thảo art style**: nên chọn phong cách low-poly/stylized để giảm tải GPU trên di động
5. **Lập ngân sách & timeline nhân sự** (nếu làm nhóm): Programmer, 3D Artist, Level Designer, UI/UX

### Giai đoạn 1 — Prototype

Mục tiêu: chứng minh "core loop" vui trước khi đầu tư art.

1. Dựng **Character Controller** cơ bản:
   - Sử dụng `CharacterController` hoặc Rigidbody + custom physics (khuyến nghị Rigidbody để dễ mở rộng vật lý bẫy/đẩy)
   - Cấu hình: di chuyển, nhảy (jump buffer + coyote time), trọng lực tuỳ chỉnh (không dùng gravity mặc định của Unity để kiểm soát cảm giác rơi)
2. Dựng **Input hệ thống cho mobile**:
   - Virtual joystick (dùng Unity's New Input System + on-screen control, hoặc asset như "Joystick Pack")
   - Nút nhảy, nút ability riêng biệt, hỗ trợ đa chạm (multi-touch)
3. Dựng **Camera 3rd-person** bám theo nhân vật, có auto-collision avoidance (camera không xuyên tường)
4. Test 1 "grey-box" level (khối hộp đơn giản) để đánh giá "game feel"
5. Playtest nội bộ liên tục, tinh chỉnh số liệu: tốc độ chạy, lực nhảy, gia tốc trượt

**Sản phẩm đầu ra:** 1 level test, nhân vật di chuyển được, không cần art đẹp.

### Giai đoạn 2 — Vertical Slice

Mục tiêu: 1 level hoàn chỉnh chất lượng cao đại diện cho toàn bộ game.

1. Thiết kế và dựng 1 level đầy đủ: địa hình, bẫy, kẻ địch cơ bản, vật phẩm thu thập
2. Thêm 2–3 ability đầu tiên (VD: double jump, dash, glide)
3. Thêm hệ thống enemy AI cơ bản (NavMesh + simple state machine: patrol → chase → attack)
4. Thêm UI: HUD (số coin, mạng, mini-map nếu cần), menu chính, màn chọn level
5. Thêm âm thanh & nhạc nền cơ bản
6. Test hiệu năng trên thiết bị Android thật (không chỉ trên Editor/PC) — đây là bước quan trọng hay bị bỏ qua

### Giai đoạn 3 — Production (phần lớn thời gian dự án)

1. Nhân bản quy trình vertical slice để sản xuất toàn bộ level còn lại
2. Xây dựng **Level Editor cho người chơi** (tính năng đặc trưng của Vista World):
   - Chế độ "Build Mode": kéo-thả đối tượng bằng chạm, xoay/scale bằng gesture (pinch, drag)
   - Palette các đối tượng: địa hình, bẫy, bệ di chuyển, checkpoint, vật phẩm
   - Serialize level thành dữ liệu (JSON) để lưu và tải lại
   - Cân nhắc dùng ScriptableObject hoặc custom JSON schema, tránh serialize toàn bộ Scene (nặng và khó đồng bộ)
3. Xây dựng **hệ thống chia sẻ level (backend)**:
   - Cần server backend: Firebase (Firestore + Storage) là lựa chọn nhanh cho indie, hoặc PlayFab, hoặc backend tự viết (Node.js/.NET) nếu cần kiểm soát nhiều hơn
   - Chức năng: upload level, tải danh sách level cộng đồng, rating/like, báo cáo nội dung không phù hợp (moderation) — bắt buộc nếu có UGC public
4. Hệ thống lưu game (save/load) local: PlayerPrefs cho dữ liệu nhỏ, file JSON/SQLite cho dữ liệu lớn (tiến trình, level đã tạo)
5. Cân nhắc monetization (nếu free-to-play): quảng cáo (rewarded ads), IAP (mở khoá skin/ability), hoặc trả phí một lần
6. Tối ưu hoá liên tục:
   - Dùng LOD (Level of Detail) cho model 3D
   - Baked lighting thay vì realtime lighting toàn bộ
   - Object pooling cho enemy/particle/projectile
   - Occlusion culling để không render phần map không nhìn thấy
   - Giảm draw call bằng batching/atlas texture

### Giai đoạn 4 — Polish & Tối ưu hoá

1. Test trên nhiều dòng máy Android (thấp/trung/cao cấp) — phân mảnh thiết bị Android là rủi ro lớn nhất
2. Tinh chỉnh input lag, đảm bảo FPS ổn định (mục tiêu tối thiểu 30 FPS ổn định, lý tưởng 60 FPS trên máy tầm trung)
3. Kiểm tra pin/nhiệt độ khi chơi liên tục 15-30 phút
4. Thêm hiệu ứng juice: screen shake nhẹ, particle khi nhặt vật phẩm, âm thanh phản hồi
5. Localization (đa ngôn ngữ) nếu nhắm thị trường quốc tế
6. Kiểm thử UGC: đảm bảo level người dùng tạo không crash game, giới hạn số lượng object để tránh lag

### Giai đoạn 5 — Soft Launch & Launch

1. **Soft launch** ở 1-2 thị trường nhỏ trước (VD: Philippines, Canada) để thu thập dữ liệu retention/crash
2. Theo dõi công cụ: Firebase Analytics/Crashlytics, hoặc GameAnalytics
3. Sửa lỗi, cân bằng độ khó dựa trên dữ liệu thật
4. Chuẩn bị trang Google Play Store: mô tả, trailer, ảnh chụp màn hình, ASO (App Store Optimization)
5. Full launch + kế hoạch cập nhật nội dung định kỳ (level mới, sự kiện cộng đồng chia sẻ level)

---

## 6. Ngăn xếp công nghệ đề xuất (Tech Stack)

| Hạng mục | Đề xuất |
|---|---|
| Game Engine | Unity 2022 LTS+ với URP |
| Ngôn ngữ | C# |
| Input | Unity Input System (mới) + custom on-screen controls |
| Backend UGC | Firebase (Firestore, Storage, Auth) hoặc PlayFab |
| Analytics | Firebase Analytics + Crashlytics |
| Version Control | Git + Git LFS (bắt buộc vì nhiều asset binary lớn) |
| Quản lý dự án | Trello/Jira/Notion cho task tracking |

---

## 7. Rủi ro chính cần lưu ý

1. **Phân mảnh thiết bị Android**: hàng nghìn cấu hình phần cứng khác nhau → phải test rộng, đặt mức đồ hoạ tối thiểu hợp lý
2. **Level Editor trên mobile khó dùng hơn PC nhiều**: cần đầu tư UX riêng, không chỉ port UI PC sang mobile
3. **UGC cần kiểm duyệt nội dung**: level không phù hợp, spam, hoặc vi phạm bản quyền do người dùng tạo — cần cơ chế report/ban
4. **Bản quyền**: không sao chép trực tiếp asset, nhân vật, tên gọi, logo, hay bất kỳ nội dung độc quyền nào của Vista World; chỉ lấy cảm hứng ở tầng cơ chế gameplay (vốn là thể loại chung, không sở hữu độc quyền)
5. **Hiệu năng pin/nhiệt**: game 3D nặng có thể khiến máy nóng nhanh, gây trải nghiệm tệ và review xấu

---

## 8. Gợi ý bước tiếp theo ngay bây giờ

1. Viết GDD chi tiết (bao gồm bảng số liệu nhân vật: tốc độ, lực nhảy, gravity...)
2. Cài Unity, tạo project rỗng với URP template cho Android
3. Dựng prototype character controller trong 1 scene trắng trong 1-2 tuần đầu, playtest trước khi làm gì khác

---

*Tài liệu này là bản tổng quan chiến lược; có thể mở rộng thêm GDD chi tiết, bảng cân bằng số liệu, hoặc kiến trúc code cụ thể (class diagram) nếu cần.*
