namespace Bewegdeal.Enums
{
    public readonly struct BrevoStatusEnum
    {
        public string Value { get; }
        private BrevoStatusEnum(string value) => Value = value;

        public static readonly BrevoStatusEnum Sent = new("sent");
        public static readonly BrevoStatusEnum Failed = new("failed");
    }
}
