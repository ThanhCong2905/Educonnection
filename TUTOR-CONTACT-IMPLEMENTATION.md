# Hồ sơ gia sư và yêu cầu liên hệ

## Thay đổi nghiệp vụ

- Gia sư không đăng nhu cầu dạy nữa. Các URL tạo nhu cầu dạy cũ bị chặn; menu thay bằng **Điều kiện nhận lớp**. Nhu cầu dạy đã lưu vẫn được giữ trong database để không mất dữ liệu lịch sử, nhưng không xuất hiện trong tìm kiếm.
- Người học tìm hồ sơ chuyên môn đã duyệt, đầy đủ minh chứng và điều kiện nhận lớp. Gia sư tiếp tục tìm nhu cầu học của học sinh/phụ huynh.
- Gia sư phải hoàn thiện chuyên môn, ảnh, môn dạy, minh chứng, điều kiện nhận lớp và được duyệt trước khi liên hệ, nhắn tin hoặc kết nối. Cấu hình bỏ qua duyệt trước đây không mở được các quyền này.

## Hai phần chỉnh sửa

**Hồ sơ chuyên môn** tại `/HoSoGiaSu/Sua`: tên hiển thị, ảnh JPG/PNG tối đa 2 MB, giới thiệu, trình độ, trường, chuyên ngành, đang học/đã tốt nghiệp, kinh nghiệm, môn và phạm vi dạy. Dữ liệu đang có được điền sẵn. Phần này gửi thành bản chờ duyệt riêng; không ghi đè bản đã duyệt. Bản bị từ chối cũng được điền lại để gia sư tiếp tục sửa.

**Điều kiện nhận lớp** tại `/HoSoGiaSu/DieuKien`: trực tuyến/trực tiếp, khu vực chung, học phí theo giờ/buổi/tháng hoặc thỏa thuận, lịch rảnh, trạng thái nhận học viên. Lưu có hiệu lực ngay và không đổi trạng thái hay phiên bản bản chuyên môn chờ duyệt. Không cho lịch trùng hoặc giờ kết thúc trước giờ bắt đầu. Khi bật nhận học viên cần có lịch rảnh.

Môn, phạm vi dạy và chuyên môn chỉ được công khai sau khi duyệt. Hồ sơ hiển thị học phí và lịch rảnh hiện tại. Email, số điện thoại, địa chỉ chi tiết và tệp minh chứng không xuất hiện trong hồ sơ công khai.

## Duyệt hồ sơ

Tài khoản có vai trò `Admin` hoặc `QuanTriVien` vào `/QuanTri/HoSo`. Có thể xem bản chờ, tải minh chứng đúng bản và duyệt/từ chối. Từ chối cần lý do. Tài khoản thường không được dùng các endpoint này; service kiểm tra lại quyền trong database. Không tự cấp quyền quản trị cho bất kỳ tài khoản ứng dụng nào.

Hồ sơ thiếu minh chứng hoặc có môn đã ngừng dùng không được duyệt. Duyệt cập nhật đồng thời thông tin và môn dạy; từ chối bản sửa không thu hồi bản đã duyệt. Minh chứng đã dùng để duyệt được giữ lại; thêm minh chứng mới không tự thu hồi duyệt của hồ sơ cũ.

## Liên hệ trước, kết nối lớp sau

1. Một bên gửi **yêu cầu liên hệ** với lời nhắn. Gửi lại hoặc gửi chéo không tự chấp nhận yêu cầu đang chờ.
2. Chỉ người nhận được chấp nhận/từ chối; người gửi được rút yêu cầu. Kiểm tra phiên bản để tránh xử lý bản cũ.
3. Chấp nhận mở cuộc trò chuyện riêng. Trước thời điểm đó, phần kết nối bị làm mờ và vô hiệu hóa; gọi thẳng API cũng bị chặn.
4. Sau khi liên hệ được chấp nhận, hai bên mới nhắn tin và gửi yêu cầu kết nối lớp. Đề xuất/chấp nhận lớp vẫn là bước riêng.

Yêu cầu liên hệ được quản lý trong **Liên hệ & kết nối**. Tin nhắn cũ không bị xóa; cuộc trò chuyện cũ cũng cần hai bên chấp nhận liên hệ để tiếp tục dùng theo quy tắc mới.

Trong chat: **Enter gửi**, **Shift+Enter xuống dòng**. Enter trong lúc bộ gõ đang composition không gửi. Khi đang gửi không gửi lặp; thử lại sau lỗi mạng giữ mã thao tác để chống trùng.

## Database

Database mới chạy lần lượt `01_TaoCoSoDuLieu.sql`, `02_DanhMucMonHoc.sql`, `03_HoSoCaNhan.sql`, `04_HoSoGiaSuVaLienHe.sql`. Database đang có chỉ cần chạy `04` trên đúng database đích. Script có thể chạy lại, không xóa bảng hay dữ liệu cũ.

`04` bổ sung thông tin chuyên môn và các bảng `DieuKienNhanLopGiaSu`, `KhungGioRanhGiaSu`, `YeuCauCapNhatHoSoGiaSu`, `YeuCauLienHe`. Các trường học phí/khu vực cũ trên `HoSoGiaSu` được giữ để tương thích lịch sử; giao diện mới đọc điều kiện từ bảng riêng.

Đã áp dụng `04` vào database local `KetNoiGiaSu`. Hồ sơ gia sư cũ cần bổ sung các thông tin bắt buộc mới và lịch rảnh; không tự điền hay tự duyệt thay gia sư.

Ảnh đại diện được lưu tại `wwwroot/images/tutors`; tệp minh chứng vẫn ở `.private-uploads`, có kiểm tra quyền khi tải. Hai thư mục cần được lưu bền khi triển khai và không đưa lên Git.

## Kiểm chứng

- Build không có lỗi hoặc cảnh báo.
- 36 kiểm thử SQL/nghiệp vụ: quyền liên hệ, chat/kết nối, phiên bản dữ liệu, điều kiện, lịch trùng, tìm hồ sơ, duyệt/từ chối và giữ bản công khai.
- `tests/Workspace.Ui/tutor-workflow.cjs`: Edge với học viên/gia sư/quản trị trên database riêng; kiểm tra ảnh, minh chứng riêng tư, duyệt, liên hệ, chat, Enter/Shift+Enter/IME, cập nhật học phí, bản sửa và giao diện 360 px.
- Script migration đã chạy thử hai lần trên database riêng trước khi áp dụng local. Số hồ sơ, nhu cầu và tin nhắn local được giữ nguyên.
