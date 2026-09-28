namespace Bewegdeal.Enums
{
    public readonly struct EmailEnum
    {
        public string Value { get; }
        private EmailEnum(string value) => Value = value;

        public static readonly EmailEnum VerifyAccount = new("Ihr Bewegdeal-Konto bestätigen # VerifyAccount");
        public static readonly EmailEnum PasswordReset = new("Ihr Bewegdeal-Passwort zurücksetzen # PasswordReset");
    }
}