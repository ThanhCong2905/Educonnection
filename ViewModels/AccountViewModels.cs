using System.ComponentModel.DataAnnotations;

namespace KetNoiGiaSu.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [RegularExpression("^(HocSinh|PhuHuynh|GiaSu)$", ErrorMessage = "Vai trò không hợp lệ.")]
    public string Role { get; set; } = "HocSinh";
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Họ và tên cần từ 2 đến 150 ký tự.")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256, ErrorMessage = "Email không được quá 256 ký tự.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^(0|\+84|84)[0-9\s.\-]{8,14}$", ErrorMessage = "Vui lòng nhập số điện thoại Việt Nam hợp lệ.")]
    public string Phone { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(64, MinimumLength = 15, ErrorMessage = "Mật khẩu cần từ 15 đến 64 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần đồng ý điều khoản và chính sách bảo mật.")]
    public bool AcceptTerms { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại.")]
    [StringLength(256, ErrorMessage = "Thông tin đăng nhập quá dài.")]
    public string Identifier { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(64, ErrorMessage = "Mật khẩu không được quá 64 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email đã đăng ký.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256)]
    public string Email { get; set; } = "";
}

public class ResetPasswordViewModel
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";
    [Required]
    public string Code { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(64, MinimumLength = 15, ErrorMessage = "Mật khẩu cần từ 15 đến 64 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [StringLength(64)]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(64, MinimumLength = 15, ErrorMessage = "Mật khẩu cần từ 15 đến 64 ký tự.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}
