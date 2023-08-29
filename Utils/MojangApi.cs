using spapp_backend.Core.Dtos;

namespace spapp_backend.Utils
{
  public class MojangApi : IDisposable
  {
    readonly HttpClient httpClient = new();

    public void Dispose()
    {
      httpClient.Dispose();
    }

    public async Task<MojangUserDataDto> GetUserByLogin(string login)
    {
      var response = await httpClient.GetAsync($"https://api.mojang.com/users/profiles/minecraft/{login}");
      var res = await response.Content.ReadFromJsonAsync<MojangUserDataDto>() ?? throw new Exception("Ошибка получения данных об игроке из Mpjang");
      return res;
    }
  }
}
