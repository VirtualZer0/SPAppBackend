namespace spapp_backend.Core.Enums
{
  public enum Role
  {
    Superadmin = 1,
    Admin = 2,
    User = 3,
    WorkerSupport = 4,
    WorkerDelivery = 5,
  }

  public static class AuthRoles
  {
    public const string Superadmin = "1";
    public const string Admin = "2";
    public const string User = "3";
    public const string WorkerSupport = "4";
    public const string WorkerDelivery = "5";
  }
}
