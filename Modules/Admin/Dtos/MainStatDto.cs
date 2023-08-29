namespace spapp_backend.Modules.Admin.Dtos
{
  public class MainStatDto
  {
    public int AllUserCount { get; set; }
    public int NewUserLastWeekCount { get; set; }
    public int NewUserTodayCount { get; set; }

    public int MonthActiveUserCount { get; set; }
    public int WeekActiveUserCount { get; set; }
    public int DayActiveUserCount { get; set; }

    public int PaymentCount { get; set; }
    public int PaymentLastWeekCount { get; set; }
    public int PaymentTodayCount { get; set; }
    public int PaymentTodayFailedCount { get; set; }

    public int? SPCardBalance { get; set; } = null!;
    public int? SPMCardBalance { get; set; } = null!;
  }
}
