public class CardPaymentRequest
{
    public int PedidoId { get; set; }
    public string Token { get; set; } = "";
    public string PaymentMethodId { get; set; } = "";
    public int Installments { get; set; } = 1;

    public string PayerEmail { get; set; } = "";
    public string IdentificationType { get; set; } = "CPF";
    public string IdentificationNumber { get; set; } = "";
}
