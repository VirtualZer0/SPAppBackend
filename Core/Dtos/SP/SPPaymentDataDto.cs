namespace spapp_backend.Core.Dtos.SP
{
    public class SPPaymentDataDto
    {
        public uint Amount { get; set; }
        public string RedirectUrl { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}
