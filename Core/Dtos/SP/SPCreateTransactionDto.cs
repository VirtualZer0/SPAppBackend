namespace spapp_backend.Core.Dtos.SP
{
    public class SPCreateTransactionDto
    {
        public string Receiver { get; set; } = string.Empty;
        public uint Amount { get; set; } = 0;
        public string Comment { get; set; } = string.Empty;
    }
}
