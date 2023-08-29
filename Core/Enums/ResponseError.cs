namespace spapp_backend.Core.Enums
{
  public enum ResponseError
  {
    Unknown,
    ServerNotAvailable,
    Forbidden,

    /// <summary>
    /// Incorrect authorization method
    /// </summary>
    AuthMethod,

    /// <summary>
    /// Requested user not found
    /// </summary>
    UserNotFound,

    /// <summary>
    /// Captcha verification was failed
    /// </summary>
    BadCaptcha,

    /// <summary>
    /// Discord authorization was failed
    /// </summary>
    AuthDiscord,

    /// <summary>
    /// Uploaded file too large
    /// </summary>
    FileTooLarge,

    /// <summary>
    /// User creating was failed
    /// </summary>
    UserCreating,

    /// <summary>
    /// SP user does not exists
    /// </summary>
    SPUserNotFound,

    /// <summary>
    /// Mojang user does not exists
    /// </summary>
    MojangUserNotFound,

    /// <summary>
    /// Too many files in one upload request
    /// </summary>
    WrongFilesCount,

    /// <summary>
    /// Too many files in one upload request
    /// </summary>
    WrongFileFormat,

    /// <summary>
    /// This user has uploaded too many files in a short time
    /// </summary>
    TooManyFilesInShortTime,

    /// <summary>
    /// Request payment not found
    /// </summary>
    PaymentNotFound,

    /// <summary>
    /// Wrong payment hash
    /// </summary>
    WrongPaymentHash,

    /// <summary>
    /// Some of fields incorrect
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// Requested account not found
    /// </summary>
    AccountNotFound,

    /// <summary>
    /// Not enough money
    /// </summary>
    InsufficientFunds,

    /// <summary>
    /// Error on SP API request
    /// </summary>
    SPApiError,

    /// <summary>
    /// Too many payments for short time, spam possible
    /// </summary>
    TooManyUnprocessedPayments,

    /// <summary>
    /// Crowdfunding company not found
    /// </summary>
    CrowdCompanyNotFound,

    /// <summary>
    /// Too many comments for short time
    /// </summary>
    TooManyComments,

    /// <summary>
    /// User was banned for this action
    /// </summary>
    ForbiddenAction,

    /// <summary>
    /// The user cannot support their own company
    /// </summary>
    CantSupportYourOwnCompany
  }
}
