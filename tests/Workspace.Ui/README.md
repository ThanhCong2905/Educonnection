# Kiểm tra giao diện workspace

Các bài này chạy sau kiểm thử Service/SQL trong `tests/Workspace.Tests`. Yêu cầu Node.js >=20, Playwright và Microsoft Edge cài trên Windows.

Chạy các lệnh từ thư mục gốc dự án. Các script chỉ dùng database kiểm thử `KetNoiGiaSu_UiTests_<guid>`, không dùng database phát triển.

1. `npm install --prefix tests/Workspace.Ui`
2. `powershell -NoProfile -ExecutionPolicy Bypass -File tests/Workspace.Ui/database.ps1 -Action Create`
3. Đọc `tmp/workspace-ui-db.json`; dùng ConnectionString trong đó làm biến môi trường `ConnectionStrings__DefaultConnection`. Chạy ứng dụng ở `http://localhost:5198`, môi trường Development. Không mở bản đang dùng database KetNoiGiaSu ở cổng này để chạy test.
4. Từ thư mục gốc: `node tests/Workspace.Ui/run.cjs`.
5. Dừng tiến trình test server, rồi chạy `powershell -NoProfile -ExecutionPolicy Bypass -File tests/Workspace.Ui/database.ps1 -Action Cleanup`.

Kịch bản tạo tài khoản và dữ liệu giả trong database kiểm thử; mỗi lần chạy cần tạo lại database rỗng. Lệnh Approve chỉ mô phỏng quản trị viên duyệt hồ sơ trong database kiểm thử, không phải chức năng quản trị của sản phẩm.
