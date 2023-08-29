namespace spapp_backend.Core.Dtos.SP
{
    public class SPPaymentResponse
    {
        public string Payer { get; set; } = string.Empty;
        public uint Amount { get; set; } = 0;
        public string Data { get; set; } = string.Empty;
    }
}
