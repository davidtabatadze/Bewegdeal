namespace Bewegdeal.Enums
{
    public class AnnotationEnum
    {
        public class Account
        {
            public class Login
            {
                public const string Credentials = "Invalid email or password.";
                public const string Blocked = "Your account has been blocked. Please, contact support.";
                public const string Pending = "Your account is pending approval. Please, wait for confirmation.";
            }
            public class ForgotPassword
            {
                public const string Success = "If email is registered, you'll receive a reset link shortly.";
            }
            public class ResetPassword
            {
                public const string Expired = "Your password reset link has expired or is invalid. Please request a new one.";
                public const string Success = "Your password has been reset. You can now log in.";
            }
            public class VerifyEmail
            {
                public const string Expired = "Verification code has expired. Please, request a new one.";
                public const string Invalid = "Invalid verification code. Please, reenter or request a new one.";
                public const string Success = "Your email has been verified. You may sign in now.";
                public const string Resent = "A new verification code has been sent to your email.";
            }
            public class Register
            {
                public const string Exists = "An account with this email address already exists. You may sign in instead.";
            }
            public class Email
            {
                public const string Verification = "We are sorry, we are unable to send you a verification email right now. Please, try again later or contact the site administration.";
                public const string Reset = "We are sorry, we are unable to send you a password reset email right now. Please, try again later or contact the site administration.";
            }
        }
        public class Request
        {
            public class Requirement
            {
                public const string Title = "Title";
                public const string ServiceType = "Service Type";
                public const string SourceAddress = "Source Address";
                public const string DestinationAddress = "Destination Address";
                public const string ProposedCost = "Proposed Cost (1 to 10,000)";
                public const string ProposedDate = "Proposed Date";
                public const string ProposedTime = "Proposed Time";
                public const string Error = "{0} field is required.";
            }
            public class Media
            {
                public const string ImageMinCount = "Image field is required.";
                public const string ImageMaxCount = "Maximum {0} images allowed.";
                public const string VideoMaxCount = "Maximum {0} videos allowed.";
            }
        }
    }
}
