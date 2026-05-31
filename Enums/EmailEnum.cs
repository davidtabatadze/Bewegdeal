namespace Bewegdeal.Enums
{
    public readonly struct EmailEnum
    {
        public string Value { get; }
        private EmailEnum(string value) => Value = value;

        public static readonly EmailEnum VerifyAccount = new("Verify your Bewegdeal account");
        public static readonly EmailEnum PasswordReset = new("Reset your Bewegdeal password");
    }
}