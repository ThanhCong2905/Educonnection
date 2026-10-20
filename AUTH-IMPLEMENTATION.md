# Tài khoản EduConnect

## Chạy local

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\run.ps1
```

Trang chủ: http://localhost:5187. Các route tài khoản ở `/Account/Register`, `/Account/Login`, `/Account/ForgotPassword`, `/Account/ChangePassword`, `/Account/Overview`.

## Thiết kế và phạm vi

- Nguồn giao diện: `design-reference/auth`, giải nén từ `KLTN (Copy).zip`. Các SVG gốc được giữ lại làm tham chiếu; trang web dùng HTML, form thật và CSS responsive.
- Font Be Vietnam Pro được lưu local; giấy phép tại `wwwroot/fonts/OFL.txt`.
- Đăng ký Học sinh/Phụ huynh/Gia sư; không cho tự đăng ký Admin. Tạo tài khoản và gán đúng một vai trò trong cùng giao dịch.
- Email và số điện thoại chuẩn hóa không được trùng. Chấp nhận cách viết số `0...`, `84...`, `+84...`, lưu dạng `0...`.
- Đăng nhập email hoặc điện thoại, chuyển hướng nội bộ, trạng thái sai thông tin, tài khoản khóa, ghi nhớ cookie và đăng xuất bằng POST.
- Quên mật khẩu dùng email, token Identity 30 phút, vô hiệu sau đặt lại thành công. Đổi/đặt lại mật khẩu thu hồi tất cả phiên.
- Mật khẩu 15–64 ký tự theo SRS, cho phép khoảng trắng, không ép loại ký tự; PBKDF2 mặc định của Identity. Bộ chặn cục bộ dùng các mục đủ độ dài từ danh sách 100k NCSC trong SecLists, tải 20/10/2026. Cần duy trì cập nhật danh sách khi vận hành.
- Hai bộ đếm SQL theo tài khoản và IP: lần sai 5/6/7/≥8 chờ 30/60/120/300 giây. Alias email/điện thoại dùng cùng khóa tài khoản. Yêu cầu đến sớm trả 429, không tăng/gia hạn; đăng nhập đúng xóa bộ đếm tài khoản, không xóa IP. Hết hạn bộ đếm sau 15 phút không sai mới.
- Giới hạn HTTP bổ sung 10 POST tài khoản/phút/IP. Với nhiều tiến trình ứng dụng, giới hạn HTTP cần kho dùng chung; bộ đếm đăng nhập nghiệp vụ đã nằm trong SQL.
- Phiên lưu bảng `PhienDangNhap`, hết hạn sau 30 phút không hoạt động. Cookie ghi nhớ không vượt qua hạn này. Kiểm tra SecurityStamp, trạng thái tài khoản và vai trò ở mỗi yêu cầu xác thực; request nền gắn `X-Background-Request: true` không gia hạn phiên.

## Database hiện có

`AppDbContext` hiện ánh xạ **phần tài khoản**: các bảng Identity, `PhienDangNhap`, `TrangThaiThuDangNhap`. Đây chưa phải model đầy đủ của toàn hệ thống. Không gọi `EnsureCreated`, `Migrate` hoặc chạy lại script tạo database. Không thay đổi schema của database hiện tại. Các vai trò được tạo khi đăng ký lần đầu nếu chưa tồn tại.

## Email

Trong Development, `Email:UsePickupDirectory=true` ghi email `.eml` ở `.maildrop`, ngoài `wwwroot`. Mở email mới nhất để lấy liên kết thử nghiệm; không công khai thư mục này. Không có liên kết đặt lại mật khẩu trong giao diện hay log.

`Account:PublicBaseUrl` là URL gốc cố định dùng tạo liên kết email, mặc định local `http://localhost:5187`. Đổi cấu hình nếu chạy cổng khác. Không lấy Host do client gửi để tạo URL.

Để gửi email thật, cấu hình bằng biến môi trường hoặc secret store:

```text
Account__PublicBaseUrl=https://your-domain.example
Email__UsePickupDirectory=false
Email__Host=your-smtp-host
Email__Port=587
Email__From=your-sender-address
Email__Username=your-smtp-user
Email__Password=your-smtp-secret
ConnectionStrings__DefaultConnection=your-sql-connection
```

Production luôn dùng SMTP qua TLS và cookie Secure. Khi SMTP lỗi, trả thông báo trung lập để không tiết lộ email đã đăng ký; ghi loại lỗi và TraceId để vận hành kiểm tra. Chưa có hàng đợi retry SMTP ở module tài khoản này.

## Kiểm tra

Kiểm tra HTTP dùng tài khoản giả `auth-test-*@example.invalid`, gồm đăng ký hợp lệ/không hợp lệ, trùng thông tin, chặn Admin, CSRF, đăng nhập email/điện thoại, redirect ngoài, phiên, logout, đổi/đặt lại mật khẩu, email local, token một lần và backoff qua alias.

Chạy lại kiểm tra HTTP từ thư mục dự án, khi bản Development đang nghe ở cổng 5198:

```powershell
node tests/auth/auth-flow-test.cjs
node tests/auth/auth-edge-test.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File tests/auth/cleanup.ps1
```

Kiểm tra edge sẽ chờ hết 30 giây backoff từ bộ kiểm tra trước. IP loopback của mỗi lần chạy được tạo riêng để không ảnh hưởng bộ đếm IP dùng bình thường. Cleanup chỉ xóa tài khoản giả và trạng thái IP của lần kiểm tra ghi trong `tmp/auth-test-identity.json`; chạy cleanup trước lần kiểm tra mới.

Các trang điều khoản/chính sách/liên hệ là nội dung giới thiệu ban đầu để liên kết điều hướng hoạt động; cần nhóm dự án hoàn thiện nội dung chính thức và kênh liên hệ trước khi phát hành.

Các module tìm kiếm, hồ sơ gia sư, nhu cầu, lớp học và quyền lớp chưa được triển khai trong phạm vi màn hình tài khoản.
