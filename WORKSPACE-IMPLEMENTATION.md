# Giao diện theo vai trò

## Mở ứng dụng

Chạy `powershell -NoProfile -ExecutionPolicy Bypass -File .\run.ps1`, mở http://localhost:5187 rồi đăng nhập.
Học sinh, Phụ huynh và Gia sư được chuyển đến `/Workspace` sau đăng nhập. Khách chưa đăng nhập vẫn xem trang giới thiệu hiện có.

## Các trang

| Route | Học sinh / Phụ huynh | Gia sư |
|---|---|---|
| `/Workspace` | Trang chủ, tìm kiếm nhanh, gợi ý tạo nhu cầu | Trang chủ, hướng dẫn xét duyệt, tạo nhu cầu dạy |
| `/Workspace/Search` | Nhu cầu nhận lớp từ gia sư đã duyệt | Nhu cầu học đang mở, không công khai tên/tuổi người được học |
| `/Workspace/Needs` | Danh sách nhu cầu học của chính mình | Danh sách nhu cầu nhận lớp của chính mình |
| `/Workspace/EditNeed` | Tạo nhu cầu, lưu nháp hoặc mở | Chỉ được tạo/sửa sau khi hồ sơ đã duyệt |
| `/Workspace/Profile` | Hồ sơ tài khoản riêng | Hồ sơ chuyên môn của chính mình |
| `/Workspace/Profile?id=...` | Xem hồ sơ gia sư đã duyệt | Xem hồ sơ được phép truy cập |
| `/Workspace/Need/{id}` | Không được truy cập màn hình dành cho gia sư | Chi tiết nhu cầu học đang mở |
| `/Workspace/EditProfile` | Không được truy cập | Tạo/sửa và gửi lại hồ sơ xét duyệt |
| `/Workspace/Status` | Vị trí theo dõi xử lý | Trạng thái xét duyệt hồ sơ và lý do từ chối |

Avatar ở cuối sidebar mở Hồ sơ cá nhân / Trạng thái xử lý / Đổi mật khẩu / Đăng xuất. Mobile thu gọn điều hướng vào Menu.

## Dữ liệu và phạm vi

- Razor Views, Bootstrap và JavaScript thuần; EF Core + SQL Server; Controller → WorkspaceService → AppDbContext.
- Ánh xạ bảng hiện có: MonHoc, HoSoGiaSu, HoSoGiaSuMonDay, NhuCauHoc, KhungGioRanh. Không chạy migration hoặc tạo lại database đang dùng.
- Script `02_DanhMucMonHoc.sql` chỉ thêm 9 môn học cơ bản nếu chưa có. Không thêm tài khoản, hồ sơ, đánh giá hoặc nhu cầu mẫu vào database phát triển.
- Quy ước: LoaiNhuCau 0=học, 1=nhận lớp; HinhThucHoc 0=trực tuyến, 1=trực tiếp; TrangThai nhu cầu 0=nháp, 1=mở, 2=đóng; duyệt hồ sơ 0=chờ, 1=đã duyệt, 2=từ chối.
- Tài khoản phải hoạt động; hồ sơ gia sư phải được duyệt tại thời điểm ghi nhu cầu, không chỉ kiểm tra trên giao diện. Chỉ công khai nhu cầu nhận lớp của gia sư đang hoạt động và đã duyệt.
- Sửa hồ sơ gia sư chuyển hồ sơ về chờ duyệt và xóa người/thời điểm duyệt cũ. Nhu cầu đang mở của hồ sơ đó tạm không xuất hiện trong tìm kiếm.
- Nhu cầu có môn học, khối lớp, mục tiêu, hình thức, học phí + đơn vị, số buổi và khung giờ. Phụ huynh khai người được học riêng trong từng nhu cầu; tên/tuổi không được đưa vào DTO tìm kiếm.
- Tạo/sửa nhu cầu và thay lịch rảnh cùng transaction. Sửa kiểm tra RowVersion; đóng nhu cầu giữ nguyên lịch sử. Biểu mẫu hiện đặt một khoảng giờ chung cho các ngày chọn, có thông báo thay thế lịch khi sửa.
- So sánh tối đa 3 nhu cầu, chỉ tại trang tìm kiếm; lưu lựa chọn trong sessionStorage theo tài khoản, dùng textContent khi tạo bảng. Giá chỉ lọc cùng đơn vị, không tự quy đổi giờ/buổi/tháng.
- Trang chủ có khối gợi ý ngay dưới banner. Chưa có nhu cầu thì mời tạo nhu cầu; có nhu cầu nhưng chưa có kết quả tính thì hiển thị trạng thái đó và dẫn sang tìm kiếm. Component có trường CompatibilityPercent để hiển thị kết quả hợp lệ sau này, nhưng chưa triển khai thuật toán tính hoặc tạo phần trăm giả.
- Kết nối và tin nhắn đã triển khai; xem `COMMUNICATION-IMPLEMENTATION.md` để biết luồng đề xuất/xác nhận lớp, quyền truy cập và kiểm thử. Lớp học đã hiển thị các lớp được xác nhận. Thông báo, theo dõi báo cáo và duyệt hồ sơ Admin vẫn chưa triển khai.
- Hồ sơ chuyên môn hiện nhập thông tin cơ bản và gửi chờ duyệt; chưa có upload/xét duyệt từng bằng cấp, không công khai giấy tờ hoặc phản hồi định kỳ của lớp.

## Kiểm thử từ trong ra ngoài

Project `tests/Workspace.Tests` dùng xUnit, tạo database SQL riêng tên `KetNoiGiaSu_WorkspaceTests_<guid>` bằng script schema rồi tự xóa sau test. Không ghi vào database KetNoiGiaSu.

```powershell
.\.dotnet\dotnet.exe test tests/Workspace.Tests/Workspace.Tests.csproj -p:RestoreConfigFile=D:\Educonnection\NuGet.Config -p:OutputPath=D:\Educonnection\bin\workspace-testbuild -p:UseSharedCompilation=false
```

Máy chạy test cần quyền tạo/xóa database kiểm thử trên SQL Server. Có thể đặt `EDU_TEST_SQL_MASTER` để dùng một máy SQL dành riêng cho test.

12 ca kiểm tra Service/SQL: chưa duyệt/từ chối/chưa có hồ sơ, không tin trạng thái giả từ ViewModel, tài khoản khóa, chủ sở hữu, draft/open/closed, transaction rollback, RowVersion cũ, công khai đúng hồ sơ và xét duyệt lại sau sửa.

15 kiểm tra Playwright bổ sung xác nhận ba vai trò, hồ sơ chờ duyệt, tạo nhu cầu thật trên database UI test riêng, bộ lọc, so sánh, bảo mật dữ liệu người học, phân quyền và responsive. Database UI test được xóa sau khi hoàn tất. Ảnh kiểm tra được lưu trong `design-reference/workspace-qa`.
