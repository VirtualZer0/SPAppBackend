using spapp_backend.Core.Dtos.SP;
using spapp_backend.Core.Enums;
using spapp_backend.Core.Models;
using System;
using System.Net.Http.Headers;
using System.Text;

namespace spapp_backend.Utils
{
  public class SPApi : IDisposable
  {
    readonly Dictionary<MCServer, HttpClient> clients = new();
    readonly Dictionary<MCServer, string> authTokens = new();
    readonly Dictionary<MCServer, string> cardTokens = new();

    private long lastReqTime = 0;

    public SPApi()
    {
      clients[MCServer.SP] = new HttpClient();
      clients[MCServer.SPM] = new HttpClient();
      clients[MCServer.SP].BaseAddress = clients[MCServer.SPM].BaseAddress = new Uri("https://spworlds.ru/api/public/");
    }

    public void SetCreds(MCServer server, string id, string token)
    {
      authTokens[server] = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{id}:{token}"));
      clients[server].DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authTokens[server]);
      cardTokens[server] = token;
    }

    public async Task<SPUserDto> GetUser(ulong discordId, MCServer serv)
    {
      await CheckRateLimit();

      var user = await clients[serv].GetAsync($"users/{discordId}");
      var res = await user.Content.ReadFromJsonAsync<SPUserDto>() ?? throw new Exception("Ошибка получения пользователя");
      return res;
    }

    public async Task<SPBalanceDto> GetBalance(MCServer serv)
    {
      await CheckRateLimit();

      var user = await clients[serv].GetAsync("card");
      var res = await user.Content.ReadFromJsonAsync<SPBalanceDto>() ?? throw new Exception("Ошибка получения данных о балансе");
      return res;
    }

    public async Task SendTransaction(SPCreateTransactionDto transaction, MCServer serv)
    {
      await CheckRateLimit();
      await clients[serv].PostAsJsonAsync("transactions", transaction);
    }

    public async Task<SPPaymentUrlDto> RequestPayment(SPPaymentDataDto payment, MCServer serv)
    {
      await CheckRateLimit();
      var response = await clients[serv].PostAsJsonAsync("payment", payment);
      var res = await response.Content.ReadFromJsonAsync<SPPaymentUrlDto>() ?? throw new Exception("Ошибка создания платежа");
      return res;
    }

    private async Task CheckRateLimit()
    {
      while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastReqTime < 1100)
      {
        await Task.Delay(1000);
      };

      lastReqTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
    }
  }
}
