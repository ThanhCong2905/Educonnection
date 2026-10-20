# Kết nối và nhắn tin

## Giao diện và dữ liệu

- Chi tiết nhu cầu học hiện tên người đăng lấy từ tài khoản; không công khai tên/tuổi học sinh do phụ huynh đại diện hoặc địa chỉ chi tiết.
- Chi tiết hồ sơ gia sư bỏ select nhu cầu học không liên quan. Người dùng nhập lời nhắn tự do; khi đến từ một bài đăng, hệ thống giữ đúng ID bài đó và kiểm tra bài còn mở, thuộc một trong hai bên.
- Các mục Kết nối, Tin nhắn, Lớp học trong sidebar đã dùng trang thật, không còn trang giữ chỗ.
- Tận dụng bảng `YeuCauKetNoiLopHoc`, `CuocHoiThoai`, `TinNhan`, `LopHoc` hiện có. Không cần script sửa database cho chức năng này.

## Kết nối

1. Một bên gửi lời mời kèm lời nhắn. Chưa tạo lớp.
2. Gia sư nhập tên lớp, môn học, mục tiêu, lịch dự kiến, thời lượng, hình thức, địa điểm nếu trực tiếp và học phí/đơn vị.
3. Bên học xem và chấp nhận hoặc từ chối. Chỉ chấp nhận đề xuất hợp lệ mới tạo một lớp trong cùng transaction.
4. Có thể thu hồi lời mời/đề xuất của mình trước khi xác nhận. Chống xử lý phiên bản cũ bằng RowVersion.
5. Lời mời/đề xuất hết hạn sau 72 giờ; gửi lại đề xuất mở thời hạn phản hồi mới. BackgroundService xử lý mỗi phút và các thao tác kết nối kiểm tra lại thời hạn.

Trạng thái: 0 chờ đề xuất, 1 chờ bên học xác nhận, 2 đã chấp nhận, 3 từ chối, 4 thu hồi, 5 hết hạn.

## Nhắn tin

- Có thể trao đổi riêng trước khi gửi lời mời hoặc sau khi kết nối. Một hội thoại riêng cho mỗi cặp gia sư–bên học.
- Gửi qua form có CSRF và AJAX; cập nhật nội dung mỗi 3 giây khi tab đang mở. Đây chưa phải SignalR/WebSocket.
- Tin nhắn 1–4000 ký tự, hiển thị dạng văn bản an toàn. MaThaoTac chống trùng khi retry. Gửi lỗi giữ nội dung.
- Hiện 100 tin mới nhất, có trang xem tin cũ. Cuộc trò chuyện và yêu cầu kết nối liệt kê tối đa 100 mục gần nhất.
- Chỉ hai bên có quyền đọc/gửi. Kiểm tra vai trò từ SQL, tài khoản còn hoạt động, địa chỉ và minh chứng gia sư trước mỗi thao tác ghi. Gia sư phải được duyệt; Development có thể bỏ duyệt theo cấu hình, không bỏ điều kiện địa chỉ/minh chứng. Tài khoản bị khóa hoặc hồ sơ không còn đủ điều kiện không gửi tiếp được.
- Poll nền không gia hạn thời gian nhàn rỗi của phiên đăng nhập.

## Kiểm thử và phạm vi

- `tests/Workspace.Tests/CommunicationTests.cs`: Service + database riêng, kiểm tra xác nhận tạo đúng một lớp, quyền của hai bên, từ chối/thu hồi, hết hạn, không tự nhắn, không nhắn cùng role, không gắn nhu cầu người khác, tài khoản khóa, thiếu minh chứng, Production không bỏ duyệt và retry không trùng tin.
- `tests/Workspace.Ui/communication.cjs`: đăng ký 3 role trong database riêng, hoàn thiện hồ sơ, tạo nhu cầu, kiểm tra tên/riêng tư, gửi lời mời, đề xuất, chấp nhận, gửi tin hai chiều, CSRF, chống XSS, lưu tin và giao diện desktop/mobile.
- Chưa triển khai SignalR, trạng thái đã đọc, tệp đính kèm tin nhắn, thông báo trong mục Thông báo hoặc hủy kết nối sau khi đã tạo lớp. Lớp học hiện hiển thị điều kiện đã xác nhận; lịch dự kiến là mô tả, chưa sinh lịch/buổi học tự động.
