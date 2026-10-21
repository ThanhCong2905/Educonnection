# Cấu trúc theo sơ đồ lớp

Các chức năng đã có được chia theo nghiệp vụ. Controller nhận HTTP, kiểm tra biểu mẫu và trả View/JSON; service xử lý truy vấn và cập nhật dữ liệu. `Data/AppDbContext.cs` tiếp tục ánh xạ entity vào các bảng hiện có.

| Nghiệp vụ | Controller | Service | Thư mục View |
| --- | --- | --- | --- |
| Tài khoản | `TaiKhoanController` | `TaiKhoanService` | `Views/TaiKhoan` |
| Hồ sơ gia sư | `HoSoGiaSuController` | `HoSoGiaSuService` | `Views/HoSoGiaSu` |
| Nhu cầu học / nhận lớp | `NhuCauController` | `NhuCauService` | `Views/NhuCau` |
| Tìm kiếm | `TimKiemController` | `TimKiemService` | `Views/TimKiem` |
| Kết nối / đề xuất lớp | `KetNoiController` | `KetNoiService` | `Views/KetNoi` |
| Tin nhắn | `TinNhanController` | `TinNhanService` | `Views/TinNhan` |
| Danh sách lớp | `LopHocController` | `LopHocService` | `Views/LopHoc` |

## Thành phần dùng chung

- `WorkspaceController` chỉ giữ trang chủ, trạng thái và trang thông báo chưa triển khai.
- `WorkspaceService` tải dữ liệu dùng chung cho giao diện, không còn xử lý tìm kiếm, lưu nhu cầu hay lưu hồ sơ gia sư.
- `WorkspaceBaseController` cung cấp mã người đang đăng nhập và dữ liệu giao diện cho các controller nghiệp vụ.
- `QuyenTuongTacService` kiểm tra hai bên tương tác, vai trò, trạng thái tài khoản, điều kiện hồ sơ và khóa giao dịch. Đây là kiểm tra trước kết nối/chat, không thay thế quyền thành viên lớp học.
- `BaoTriWorker` xử lý yêu cầu kết nối hết hạn; chưa xử lý toàn bộ nghiệp vụ worker trong thiết kế đích.
- Các partial và trang chặn dùng chung nằm trong `Views/Shared`.

## Tên hàm và URL

Tài khoản dùng `DangKy`, `DangNhap`, `DangXuat`, `QuenMatKhau`, `DatLaiMatKhau`, `DoiMatKhau`. Hồ sơ gia sư dùng `ChiTiet`, `Sua`, `Luu`; service dùng `LayHoSoAsync`, `LayDeSuaAsync`, `CapNhatAsync`.

Nhu cầu có `TaoNhuCauHoc`, `TaoNhuCauNhanLop`, `CapNhat`, `Dong`. Kết nối có `GuiYeuCau`, `GuiDeXuat`, `ChapNhan`, `Rut`, `TuChoi`; các hàm service tương ứng dùng hậu tố `Async`. Phản hồi kết nối vẫn dùng chung phần kiểm tra quyền, trạng thái và phiên bản dữ liệu.

Menu và form trỏ đến controller nghiệp vụ. Các URL cũ `/Account/...` và `/Workspace/...` được giữ bằng route alias, bao gồm các URL nhận `id` ở đường dẫn. Không duy trì controller cũ chỉ để chuyển tiếp.

## Phạm vi lần tổ chức này

Không thay đổi schema hoặc dữ liệu; không sửa nội dung SQL người dùng đang chỉnh. Không bổ sung hồ sơ cá nhân vào sơ đồ. Luồng địa chỉ và tệp hồ sơ đã tồn tại trong code vẫn được giữ để tránh thay đổi điều kiện sử dụng hiện tại.

Đây chưa phải triển khai toàn bộ sơ đồ: lịch học/khung giờ/buổi học mới, bài tập, quản trị, đánh giá, thông báo, SignalR và outbox chưa được bổ sung. Entity, ViewModel, kiểu kết quả và chữ ký tham số hiện có chưa được đổi đồng loạt thành mọi tên/kiểu trong sơ đồ. Các nghiệp vụ mật khẩu vốn xử lý trong controller vẫn giữ nguyên để lần tổ chức này không thay đổi cơ chế xác thực.

## Kiểm tra

- `dotnet build --no-restore`: biên dịch ứng dụng và Razor.
- `dotnet test tests/Workspace.Tests/Workspace.Tests.csproj --no-restore`: kiểm thử SQL/nghiệp vụ với database riêng.
- `node tests/Workspace.Ui/architecture.cjs`: kiểm tra HTTP đăng ký, đăng nhập, menu, URL mới/cũ, tạo/sửa nhu cầu và CSRF. Cần máy chủ/database kiểm thử ở cổng 5198 theo `tests/Workspace.Ui/README.md`; không cần Playwright.
- Các kịch bản Playwright cũ đã cập nhật cách chờ URL và chọn form theo controller mới; lần thay đổi này chỉ kiểm tra cú pháp các kịch bản đó, không chạy trình duyệt.
