# OrderView – Thiết kế & lịch sử thay đổi

> Tài liệu sống cho `Assets/HotPot/Scripts/Customer/OrderView.cs`.
> **Quy tắc:** mỗi lần đọc, sửa hoặc thiết kế lại logic OrderView, cập nhật mục *Luật bố cục hiện hành* (nếu thay đổi) và thêm một dòng vào *Nhật ký thay đổi*.

## 1. Phạm vi
- `OrderView` – xếp các ô món (`OrderItemView`) trên thẻ order và co giãn nền (`background`, SpriteRenderer sliced).
- Dữ liệu vào: `CustomerOrder.finalFoods` (tối đa `CustomerOrder.MaxDishes = 4`).
- File liên quan: `OrderItemView.cs`, `CustomerView.cs`, `CustomerOrder.cs`.

## 2. Tham số Inspector
| Trường | Mặc định | Ý nghĩa |
|---|---|---|
| `items` | – | Các ô món theo thứ tự hiển thị (cần đủ 4) |
| `dishGridCenter` | (0,0) | Tâm cục bộ của bố cục món |
| `dishSpacing` | (0.75, 0.82) | Khoảng cách: x giữa cột, y giữa hàng |
| `dishScale` | 0.7 | Scale mỗi ô món |
| `background` | Artwork | Nền thẻ, drawMode Sliced |
| `dishCellSize` | (1.03, 0.77) | Kích thước 1 ô trước scale |
| `backgroundPadding` | (0.32, 0.3) | Đệm nền (hiện chỉ dùng x) |

## 3. Luật bố cục hiện hành
| Số món | Bố cục | Nền |
|---|---|---|
| 0 | Ẩn hết | Tắt nền |
| 1 | Trục dọc, **căn giữa** – tâm món = tâm thẻ (`dishGridCenter`) | Rộng 1 cột, giữ nguyên chiều cao |
| 2 | 1 cột, 2 hàng xếp dọc, đối xứng quanh tâm | Rộng 1 cột, giữ nguyên chiều cao |
| 3 | *(chưa chốt – hiện là lưới 2 cột: 2 trên, 1 dưới căn giữa)* | Rộng 2 cột |
| 4 | *(chưa chốt – hiện là lưới 2×2)* | Rộng 2 cột |

Công thức hiện tại (`Layout`):
- `columns = count <= 2 ? 1 : 2`, `rows = ceil(count / columns)`
- `x = center.x + ((i % columns) - (inRow - 1) / 2) * spacing.x`
- `y = center.y + ((rows - 1) / 2 - row) * spacing.y`
- Rộng nền = `cellSize.x * dishScale + padding.x + (columns - 1) * spacing.x`

## 4. Quyết định thiết kế
- [2026-10-21] Order 1 món: giữ trục dọc, căn giữa cả hai trục; chiều cao thẻ bằng thẻ 2 món. (Đã duyệt bằng mockup.)

## 5. Ý tưởng chờ xét
- 3 món: bố cục tam giác (1 trên, 2 dưới) hoặc hàng ngang.
- Scale theo số món (vd. 0.85 / 0.75 / 0.65 / 0.6).
- Chấm tiến độ nguyên liệu dưới mỗi món; món xong làm mờ + ✓.
- Tween vị trí ô khi đổi bố cục; nền dạng bong bóng chat.

## 6. Nhật ký thay đổi
| Ngày | Thay đổi | File |
|---|---|---|
| 2026-10-21 | Tạo tài liệu; ghi nhận luật hiện hành và quyết định 1 món căn giữa dọc | OrderView.md |
