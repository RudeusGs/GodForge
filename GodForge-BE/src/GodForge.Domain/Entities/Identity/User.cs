using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Identity;

public sealed class User : BaseAuditableEntity, ISoftDeletable
{
    public const int MaxEmailLength = 255;
    public const int MaxDisplayNameLength = 120;
    public const int MaxPasswordHashLength = 255;
    public const int MaxPasswordResetTokenHashLength = 255;

    public string Email { get; private set; } = default!;
    public string NormalizedEmail { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;

    public SystemRole SystemRole { get; private set; }
    public UserStatus Status { get; private set; }

    public DateTimeOffset? EmailVerifiedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset? PasswordChangedAt { get; private set; }

    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// Dùng để vô hiệu hóa các session/token cũ khi thông tin bảo mật thay đổi.
    /// Ví dụ: đổi mật khẩu hoặc reset mật khẩu.
    /// </summary>
    public string SecurityStamp { get; private set; } = default!;

    /// <summary>
    /// Thay đổi mỗi khi aggregate User bị cập nhật.
    ///
    /// Có thể dùng để kiểm soát optimistic concurrency,
    /// tránh trường hợp nhiều request cùng sửa một User.
    /// </summary>
    public string ConcurrencyStamp { get; private set; } = default!;

    /// <summary>
    /// Phiên bản hiện tại của aggregate.
    ///
    /// Mỗi lần state thay đổi thì Version sẽ tăng lên 1.
    /// Có thể dùng cho optimistic concurrency hoặc theo dõi version.
    /// </summary>
    public long Version { get; private set; }

    public string? PasswordResetTokenHash { get; private set; }
    public DateTimeOffset? PasswordResetTokenExpiry { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    // Constructor private để ngăn tạo User trực tiếp từ bên ngoài.
    // User phải được tạo thông qua factory method Create().
    private User()
    {
    }

    // ------------------------------------------------------------------------
    // Khởi tạo User
    // ------------------------------------------------------------------------

    public static User Create(
        string email,
        string displayName,
        string passwordHash,
        DateTimeOffset now)
    {
        var validatedEmail = ValidateRequiredLength(
            email,
            MaxEmailLength,
            nameof(email));

        var validatedDisplayName = ValidateRequiredLength(
            displayName,
            MaxDisplayNameLength,
            nameof(displayName));

        var validatedPasswordHash = ValidateRequiredLength(
            passwordHash,
            MaxPasswordHashLength,
            nameof(passwordHash),
            trim: false);

        return new User
        {
            Id = Guid.NewGuid(),

            Email = validatedEmail,
            NormalizedEmail = NormalizeEmail(validatedEmail),
            DisplayName = validatedDisplayName,
            PasswordHash = validatedPasswordHash,

            // User mới mặc định là người dùng thông thường.
            SystemRole = SystemRole.User,

            // User mới mặc định ở trạng thái hoạt động.
            Status = UserStatus.Active,

            FailedLoginCount = 0,
            LockedUntil = null,

            SecurityStamp = NewStamp(),
            ConcurrencyStamp = NewStamp(),

            // Aggregate mới bắt đầu từ version 1.
            Version = 1,

            CreatedAt = now,
            UpdatedAt = now
        };
    }

    // ------------------------------------------------------------------------
    // Các thuộc tính hỗ trợ kiểm tra trạng thái
    // ------------------------------------------------------------------------

    /// <summary>
    /// User đã bị soft delete hay chưa.
    /// </summary>
    public bool IsDeleted => DeletedAt is not null;

    /// <summary>
    /// Email của User đã được xác minh hay chưa.
    /// </summary>
    public bool IsEmailVerified => EmailVerifiedAt is not null;

    /// <summary>
    /// Kiểm tra tài khoản hiện tại có đang thực sự bị khóa hay không.
    ///
    /// Status có thể vẫn là Locked nhưng nếu LockedUntil đã hết hạn
    /// thì tài khoản được xem là không còn bị khóa.
    /// </summary>
    public bool IsLocked(DateTimeOffset now)
    {
        return Status == UserStatus.Locked
               && LockedUntil.HasValue
               && LockedUntil.Value > now;
    }

    /// <summary>
    /// Kiểm tra tài khoản có được phép bắt đầu quá trình đăng nhập hay không.
    ///
    /// Method này chỉ kiểm tra trạng thái của User.
    /// Nó không kiểm tra password.
    ///
    /// Việc verify password thuộc Application/Infrastructure layer.
    /// </summary>
    public bool CanAttemptLogin(DateTimeOffset now)
    {
        if (IsDeleted)
            return false;

        return Status switch
        {
            // User đang Active thì được phép thử đăng nhập.
            UserStatus.Active => true,

            // Nếu User đang mang trạng thái Locked nhưng thời gian khóa
            // đã hết thì cho phép thử đăng nhập lại.
            UserStatus.Locked => !IsLocked(now),

            // Các trạng thái khác mặc định không được đăng nhập.
            // Ví dụ: Suspended, Disabled, Deleted...
            _ => false
        };
    }

    /// <summary>
    /// Chuẩn hóa email để phục vụ lookup và unique index.
    ///
    /// Ví dụ:
    ///     Test@Email.com
    /// sẽ trở thành:
    ///     TEST@EMAIL.COM
    /// </summary>
    public static string NormalizeEmail(string email)
    {
        return ValidateRequiredLength(
                email,
                MaxEmailLength,
                nameof(email))
            .ToUpperInvariant();
    }

    // ------------------------------------------------------------------------
    // Thông tin hồ sơ
    // ------------------------------------------------------------------------

    public void UpdateDisplayName(
        string displayName,
        DateTimeOffset now)
    {
        // User đã bị xóa thì không được phép chỉnh sửa thông tin.
        ThrowIfDeleted();

        var validatedDisplayName = ValidateRequiredLength(
            displayName,
            MaxDisplayNameLength,
            nameof(displayName));

        // Nếu giá trị không thay đổi thì không cần tăng Version
        // hoặc cập nhật ConcurrencyStamp.
        if (DisplayName == validatedDisplayName)
            return;

        DisplayName = validatedDisplayName;

        Touch(now);
    }

    // ------------------------------------------------------------------------
    // Email
    // ------------------------------------------------------------------------

    public void MarkEmailVerified(DateTimeOffset now)
    {
        ThrowIfDeleted();

        // Email đã xác minh rồi thì không cần cập nhật lại.
        if (EmailVerifiedAt is not null)
            return;

        EmailVerifiedAt = now;

        Touch(now);
    }

    // ------------------------------------------------------------------------
    // Quyền hệ thống
    // ------------------------------------------------------------------------

    public void UpdateSystemRole(
        SystemRole role,
        DateTimeOffset now)
    {
        ThrowIfDeleted();

        EnumGuard.ThrowIfUndefined(role, nameof(role));

        if (SystemRole == role)
            return;

        SystemRole = role;

        /*
         * Role là thông tin liên quan trực tiếp đến quyền hạn.
         *
         * Ví dụ:
         * Admin -> User
         *
         * Nếu session/token cũ vẫn còn hiệu lực thì User có thể
         * tiếp tục giữ quyền Admin.
         *
         * Vì vậy cần đổi SecurityStamp để các session/token cũ
         * có thể bị vô hiệu hóa.
         */
        RotateSecurityStamp();

        Touch(now);
    }

    // ------------------------------------------------------------------------
    // Đăng nhập / khóa tài khoản
    // ------------------------------------------------------------------------

    public void RecordLoginSuccess(DateTimeOffset now)
    {
        ThrowIfDeleted();

        /*
         * Domain tự kiểm tra quyền đăng nhập.
         *
         * Không phụ thuộc hoàn toàn vào Application Service,
         * tránh trường hợp service khác gọi trực tiếp method này
         * và làm User rơi vào state không hợp lệ.
         */
        if (!CanAttemptLogin(now))
        {
            throw new InvalidOperationException(
                "Tài khoản hiện tại không được phép đăng nhập.");
        }

        LastLoginAt = now;

        // Đăng nhập thành công thì reset số lần đăng nhập sai.
        FailedLoginCount = 0;

        // Đồng thời xóa thời gian khóa.
        LockedUntil = null;

        /*
         * Trường hợp User trước đó bị Locked nhưng thời gian khóa đã hết,
         * khi đăng nhập thành công thì chuyển trạng thái trở lại Active.
         */
        if (Status == UserStatus.Locked)
            Status = UserStatus.Active;

        Touch(now);
    }

    public void RecordLoginFailure(
        DateTimeOffset now,
        int maxFailedAccessAttempts,
        TimeSpan lockoutDuration)
    {
        ThrowIfDeleted();

        /*
         * maxFailedAccessAttempts và lockoutDuration là policy.
         *
         * Giá trị cụ thể như:
         * - Sai 5 lần
         * - Khóa 15 phút
         *
         * nên được lấy từ config/Application layer rồi truyền vào Domain.
         */
        if (maxFailedAccessAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxFailedAccessAttempts),
                maxFailedAccessAttempts,
                "Số lần đăng nhập sai tối đa phải lớn hơn 0.");
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lockoutDuration),
                lockoutDuration,
                "Thời gian khóa tài khoản phải lớn hơn 0.");
        }

        /*
         * Nếu hiện tại tài khoản vẫn đang trong thời gian bị khóa
         * thì không nên tiếp tục ghi nhận login failure.
         *
         * Application Service nên chặn từ trước bằng CanAttemptLogin(),
         * nhưng Domain vẫn guard thêm để đảm bảo invariant.
         */
        if (IsLocked(now))
        {
            throw new InvalidOperationException(
                "Không thể ghi nhận đăng nhập thất bại khi tài khoản đang bị khóa.");
        }

        /*
         * Chặn các trạng thái không được phép đăng nhập như
         * Deleted, Disabled, Suspended...
         */
        if (!CanAttemptLogin(now))
        {
            throw new InvalidOperationException(
                "Tài khoản hiện tại không được phép thực hiện đăng nhập.");
        }

        /*
         * Trường hợp tài khoản trước đó bị Locked,
         * nhưng thời gian khóa đã hết.
         *
         * Ta bắt đầu một chu kỳ đếm lỗi mới.
         *
         * Nếu không reset FailedLoginCount ở đây,
         * User có thể vừa hết khóa và chỉ cần sai thêm 1 lần
         * là bị khóa ngay lập tức.
         */
        if (Status == UserStatus.Locked)
        {
            Status = UserStatus.Active;
            FailedLoginCount = 0;
            LockedUntil = null;
        }

        FailedLoginCount++;

        /*
         * Khi số lần đăng nhập sai đạt ngưỡng cho phép,
         * chuyển User sang trạng thái Locked.
         */
        if (FailedLoginCount >= maxFailedAccessAttempts)
        {
            Status = UserStatus.Locked;
            LockedUntil = now.Add(lockoutDuration);
        }

        Touch(now);
    }

    /// <summary>
    /// Mở khóa tài khoản thủ công.
    ///
    /// Method này phù hợp cho các trường hợp như:
    /// - Admin mở khóa User
    /// - Support mở khóa User
    /// - Một use case chủ động unlock tài khoản
    /// </summary>
    public void Unlock(DateTimeOffset now)
    {
        ThrowIfDeleted();

        // Không ở trạng thái Locked thì không cần làm gì.
        if (Status != UserStatus.Locked)
            return;

        FailedLoginCount = 0;
        LockedUntil = null;
        Status = UserStatus.Active;

        Touch(now);
    }

    // ------------------------------------------------------------------------
    // Mật khẩu
    // ------------------------------------------------------------------------

    public void UpdatePassword(
        string passwordHash,
        DateTimeOffset now)
    {
        ThrowIfDeleted();

        var validatedPasswordHash = ValidateRequiredLength(
            passwordHash,
            MaxPasswordHashLength,
            nameof(passwordHash),
            trim: false);

        PasswordHash = validatedPasswordHash;
        PasswordChangedAt = now;

        /*
         * Khi password thay đổi cần đổi SecurityStamp.
         *
         * Mục đích là để có thể vô hiệu hóa:
         * - Session cũ
         * - Refresh token cũ
         * - Các thông tin xác thực đã cấp trước khi đổi mật khẩu
         */
        RotateSecurityStamp();

        /*
         * Sau khi password đã được đổi,
         * password reset token cũ phải bị vô hiệu hóa.
         *
         * Tránh trường hợp token reset cũ vẫn còn thời hạn
         * và tiếp tục được sử dụng.
         */
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiry = null;

        Touch(now);
    }

    // ------------------------------------------------------------------------
    // Reset mật khẩu
    // ------------------------------------------------------------------------

    public void SetPasswordResetToken(
        string tokenHash,
        DateTimeOffset expiry,
        DateTimeOffset now)
    {
        ThrowIfDeleted();

        var validatedTokenHash = ValidateRequiredLength(
            tokenHash,
            MaxPasswordResetTokenHashLength,
            nameof(tokenHash),
            trim: false);

        /*
         * Token reset password phải có thời hạn trong tương lai.
         */
        if (expiry <= now)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiry),
                expiry,
                "Thời gian hết hạn của token reset mật khẩu phải nằm trong tương lai.");
        }

        PasswordResetTokenHash = validatedTokenHash;
        PasswordResetTokenExpiry = expiry;

        Touch(now);
    }

    /// <summary>
    /// Kiểm tra User hiện có password reset token hợp lệ hay không.
    ///
    /// Method này chỉ kiểm tra:
    /// - User chưa bị xóa
    /// - Có token hash
    /// - Có expiry
    /// - Token chưa hết hạn
    ///
    /// Việc so sánh token người dùng gửi lên với TokenHash
    /// nên được xử lý ở Application/Infrastructure layer.
    /// </summary>
    public bool HasValidPasswordResetToken(DateTimeOffset now)
    {
        return !IsDeleted
               && PasswordResetTokenHash is not null
               && PasswordResetTokenExpiry.HasValue
               && PasswordResetTokenExpiry.Value > now;
    }

    public void ClearPasswordResetToken(DateTimeOffset now)
    {
        // Không có token thì không cần update aggregate.
        if (PasswordResetTokenHash is null &&
            PasswordResetTokenExpiry is null)
        {
            return;
        }

        PasswordResetTokenHash = null;
        PasswordResetTokenExpiry = null;

        Touch(now);
    }

    // ------------------------------------------------------------------------
    // Soft delete
    // ------------------------------------------------------------------------

    public void SoftDelete(DateTimeOffset now)
    {
        // Soft delete phải idempotent.
        // Gọi nhiều lần vẫn chỉ có tác dụng như gọi một lần.
        if (IsDeleted)
            return;

        DeletedAt = now;
        Status = UserStatus.Deleted;

        /*
         * Tài khoản đã bị xóa thì không cần giữ trạng thái lockout.
         */
        FailedLoginCount = 0;
        LockedUntil = null;

        /*
         * Reset token cũng phải bị vô hiệu hóa
         * khi tài khoản bị xóa.
         */
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiry = null;

        /*
         * Đổi SecurityStamp để có thể vô hiệu hóa
         * các session / refresh token hiện đang tồn tại.
         */
        RotateSecurityStamp();

        Touch(now);
    }

    // ------------------------------------------------------------------------
    // Guard bảo vệ invariant
    // ------------------------------------------------------------------------

    /// <summary>
    /// Chặn các operation không được phép thực hiện
    /// trên một User đã bị soft delete.
    /// </summary>
    private void ThrowIfDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "Không thể thực hiện thao tác trên tài khoản đã bị xóa.");
        }
    }

    /// <summary>
    /// Validate một chuỗi bắt buộc phải có giá trị
    /// và không được vượt quá maximumLength.
    ///
    /// trim = true:
    ///     Tự động loại bỏ khoảng trắng đầu/cuối.
    ///
    /// trim = false:
    ///     Giữ nguyên giá trị.
    ///
    /// Với password hash hoặc token hash thì không nên trim
    /// vì đây là dữ liệu kỹ thuật, cần giữ nguyên tuyệt đối.
    /// </summary>
    private static string ValidateRequiredLength(
        string value,
        int maximumLength,
        string parameterName,
        bool trim = true)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        var normalized = trim
            ? value.Trim()
            : value;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException(
                $"{parameterName} là bắt buộc.",
                parameterName);
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{parameterName} không được vượt quá {maximumLength} ký tự.",
                parameterName);
        }

        return normalized;
    }

    // ------------------------------------------------------------------------
    // Quản lý state nội bộ
    // ------------------------------------------------------------------------

    /// <summary>
    /// Tạo SecurityStamp mới.
    ///
    /// Chỉ nên gọi khi có thay đổi liên quan đến bảo mật,
    /// ví dụ:
    /// - Đổi mật khẩu
    /// - Đổi quyền
    /// - Xóa tài khoản
    /// </summary>
    private void RotateSecurityStamp()
    {
        SecurityStamp = NewStamp();
    }

    /// <summary>
    /// Đánh dấu aggregate đã bị thay đổi.
    ///
    /// Mỗi mutation của User nên đi qua Touch()
    /// để đảm bảo:
    ///
    /// - Version tăng lên
    /// - UpdatedAt được cập nhật
    /// - ConcurrencyStamp được thay mới
    /// </summary>
    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
        ConcurrencyStamp = NewStamp();
    }

    /// <summary>
    /// Tạo stamp ngẫu nhiên mới.
    ///
    /// Dùng format "N" để UUID không chứa dấu gạch ngang.
    /// </summary>
    private static string NewStamp()
    {
        return Guid.NewGuid().ToString("N");
    }
}
