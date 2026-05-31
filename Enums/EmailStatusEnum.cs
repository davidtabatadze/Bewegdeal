namespace Bewegdeal.Enums
{
    public readonly struct EmailStatusEnum
    {
        public string Value { get; }
        private EmailStatusEnum(string value) => Value = value;

        public static readonly EmailStatusEnum Sent = new("sent");
        public static readonly EmailStatusEnum Failed = new("failed");
    }
}
