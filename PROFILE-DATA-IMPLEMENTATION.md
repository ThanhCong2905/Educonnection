# Địa chỉ cá nhân và minh chứng gia sư

Địa chỉ (tỉnh/thành phố và địa chỉ chi tiết) là bắt buộc cho cả ba role. Gia sư phải có ít nhất một tệp bằng cấp/chứng chỉ để hồ sơ đầy đủ. Thông tin chuyên môn có thể lưu trước để tải tệp; hồ sơ chưa đầy đủ bị chặn tạo/sửa nhu cầu cả trên Controller và Service, kể cả khi Development bỏ duyệt. Không thay đổi/xóa dữ liệu cũ để ép đầy đủ; người dùng được nhắc bổ sung khi sử dụng chức năng.

- Chạy `03_HoSoCaNhan.sql` sau schema gốc. Script có kiểm tra tồn tại; không chạy lại script tạo database gốc. Đã áp dụng bảng này vào database phát triển.
- Cả ba role dùng Avatar → Hồ sơ cá nhân (`/Workspace/Profile`). Địa chỉ được gộp ngay trong hồ sơ; tỉnh/thành phố, khu vực và địa chỉ chi tiết được nhập thực tế, không dùng danh mục địa phương cố định. Đường dẫn cũ `/ProfileData/Address` chuyển về hồ sơ.
- Tạo nhu cầu mới tự điền khu vực và địa chỉ từ hồ sơ; chỉnh sửa dùng dữ liệu của chính nhu cầu. Học trực tuyến không lưu địa chỉ. Thay đổi hồ sơ không thay đổi bài đã đăng.
- `NeedCard` và các luồng công khai không chứa địa chỉ chi tiết. Khu vực chung vẫn tìm kiếm được.
- Gia sư có bằng cấp, chứng chỉ, minh chứng kinh nghiệm ngay trong Hồ sơ cá nhân và trang chỉnh sửa hồ sơ, cùng phần địa chỉ. Cần lưu hồ sơ chuyên môn trước khi thêm tệp. Đường dẫn cũ `/ProfileData/Documents` chuyển về phần minh chứng trong hồ sơ; không còn mục menu/trang riêng.
- Dùng bảng `TepDinhKem` đã có. Nhận PDF/JPG/PNG với kiểm tra chữ ký đầu tệp, tối đa 10 MB/tệp, 10 tệp/hồ sơ. Không coi kiểm tra định dạng là quét mã độc.
- Tệp lưu bằng tên GUID trong `.private-uploads`, ngoài wwwroot, bị loại khỏi Git và publish. Cần sao lưu riêng thư mục này khi triển khai.
- Chỉ chủ hồ sơ có quyền liệt kê, tải xuống và xóa. Tài liệu không công khai trên trang tìm kiếm/hồ sơ. Chưa bổ sung giao diện Admin xem minh chứng hoặc dịch vụ quét mã độc; trạng thái quét là chờ xử lý.
- Thêm/xóa minh chứng chuyển hồ sơ về chờ xét duyệt. Development có thể tạm bỏ điều kiện duyệt bằng `Workspace:BypassTutorApproval`; Production bỏ qua tùy chọn này. Chưa triển khai kết nối/nhắn tin thật.
- Kiểm thử: `tests/Workspace.Tests` gồm SQL riêng biệt; `tests/Workspace.Ui/profile-data.cjs` kiểm tra địa chỉ, tự điền, không lộ địa chỉ, bỏ duyệt Development, upload/download/delete, quyền truy cập và giao diện 360px. Dùng database UI riêng từ `database.ps1`; không chạy trên dữ liệu thật. Kịch bản cũ `run.cjs` cần `Workspace__BypassTutorApproval=false` để kiểm tra điều kiện duyệt.
