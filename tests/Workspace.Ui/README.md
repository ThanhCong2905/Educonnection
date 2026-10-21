# Kiểm tra giao diện

Kịch bản chính hiện tại là `tutor-workflow.cjs`, kiểm tra luồng hồ sơ chuyên môn, điều kiện nhận lớp, xét duyệt, yêu cầu liên hệ và nhắn tin. Cần Node.js >=20, Playwright và Microsoft Edge trên Windows.

Chạy từ thư mục gốc dự án:

1. `npm install --prefix tests/Workspace.Ui`
2. `powershell -NoProfile -ExecutionPolicy Bypass -File tests/Workspace.Ui/database.ps1 -Action Create`
3. Đọc `tmp/workspace-ui-db.json`, dùng ConnectionString trong đó làm biến môi trường `ConnectionStrings__DefaultConnection`. Build và chạy ứng dụng ở `http://localhost:5198` với môi trường Development.
4. `node tests/Workspace.Ui/tutor-workflow.cjs`
5. Dừng máy chủ kiểm thử, rồi chạy `powershell -NoProfile -ExecutionPolicy Bypass -File tests/Workspace.Ui/database.ps1 -Action Cleanup`.

Không chạy kịch bản trên database phát triển. Script tự xác minh máy chủ kết nối database riêng có tên `KetNoiGiaSu_UiTests_<guid>` trước khi tạo dữ liệu.

Tài khoản quản trị trong bài kiểm tra được tạo bằng `tutor-reviewer.ps1` và chỉ được đổi vai trò bên trong database kiểm thử đã kiểm tra tên. Việc duyệt hồ sơ trong kịch bản dùng giao diện quản trị thật. Không tạo hay cấp quyền cho tài khoản ứng dụng ngoài database kiểm thử.

Kịch bản kiểm tra Enter gửi tin, Shift+Enter xuống dòng, bộ gõ IME, ảnh đại diện được tải, minh chứng không công khai, bản sửa không ghi đè bản đã duyệt, học phí cập nhật ngay và giao diện 360 px. Ảnh kiểm tra lưu trong `tmp/tutor-qa`.

`architecture.cjs` kiểm tra HTTP cơ bản, không cần trình duyệt. Các script `run.cjs`, `profile-data.cjs`, `communication.cjs` ghi lại luồng cũ trước khi bỏ nhu cầu dạy; không phải kịch bản kiểm thử chính của nghiệp vụ mới.
