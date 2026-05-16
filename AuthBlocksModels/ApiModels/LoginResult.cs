using NetBlocks.Models;
using NetBlocks.Utilities;

namespace AuthBlocksModels.ApiModels;

public enum LoginFailureReason
{
    InvalidCredentials,
    UserLockedOut,
    // UserNotConfirmed,
    UserNotActive,
    SystemError
}

public class LoginFailureReasonDisplay : DisplayEnumeration<LoginFailureReasonDisplay>
{
    public static LoginFailureReasonDisplay InvalidCredentials = new(LoginFailureReason.InvalidCredentials, "Invalid Credentials");
    public static LoginFailureReasonDisplay UserLockedOut = new(LoginFailureReason.UserLockedOut, "Locked Out");
    public static LoginFailureReasonDisplay UserNotActive = new(LoginFailureReason.UserNotActive, "Account Deactivated");
    public static LoginFailureReasonDisplay SystemError = new(LoginFailureReason.SystemError, "System Error");
    protected LoginFailureReasonDisplay(LoginFailureReason reason, string displayName) : base((int)reason, reason.ToString(), displayName)
    {
    }
}

public class LoginResult : ResultBase<LoginResult>
{
    public LoginFailureReason? FailureReason { get; set; }
    
    public static LoginResult CreateFailResult(string message, LoginFailureReason failureReason)
    {
        var result = ResultBase<LoginResult>.CreateFailResult(message);
        result.FailureReason = failureReason;
        return result;
    }

    public static LoginResult From<T>(LoginResult<T> other)
    {
        var result = ResultBase<LoginResult>.From(other);
        result.FailureReason = other.FailureReason;
        return result;
    }
}

public class LoginResultDto : ResultBase<LoginResult>.ResultDtoBase<LoginResult, LoginResultDto>
{
    public LoginFailureReason? FailureReason { get; set; }
    
    public LoginResultDto() : base() { }

    public LoginResultDto(LoginResult? result) : base(result)
    {
        FailureReason = result?.FailureReason;
    }

    public override LoginResult From()
    {
        var result = base.From();
        result.FailureReason = FailureReason;
        return result;
    }
}

public class LoginResult<T> : ResultContainerBase<LoginResult<T>, T>
{
    public LoginFailureReason? FailureReason { get; set; }
    
    public static LoginResult<T> CreateFailResult(string message, LoginFailureReason failureReason)
    {
        var result = ResultContainerBase<LoginResult<T>,T>.CreateFailResult(message);
        result.FailureReason = failureReason;
        return result;
    }
}

public class LoginResultDto<T> : ResultContainerDtoBase<LoginResultDto<T>, LoginResult<T>,T>
{
    public LoginFailureReason? FailureReason { get; set; }
    
    public LoginResultDto() : base() { }

    public LoginResultDto(LoginResult<T>? result) : base(result)
    {
        FailureReason = result?.FailureReason;
    }

    public override LoginResult<T> From()
    {
        var result = base.From();
        result.FailureReason = FailureReason;
        return result;
    }
}
