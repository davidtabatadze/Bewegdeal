namespace Bewegdeal.Enums
{
    public class AnnotationEnum
    {
        public class General
        {
            public class Service
            {
                public const string Moving = "Moving Service";
                public const string Removal = "Junk Removal";
                public const string Pickup = "Store Pickup";
                public const string Transport = "Vehicle Transport";
            }
        }
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
                public const string MobileExists = "An account with this phone number already exists. You may sign in instead.";
            }
            public class VerifyMobile
            {
                public const string Expired = "Verification code has expired. Please, request a new one.";
                public const string Invalid = "Invalid verification code. Please, reenter or request a new one.";
                public const string Success = "Your phone number has been verified. Please verify your email to continue.";
                public const string Resent = "A new verification code has been sent to your phone.";
            }
            public class Email
            {
                public const string Verification = "We are sorry, we are unable to send you a verification email right now. Please, try again later or contact the site administration.";
                public const string Reset = "We are sorry, we are unable to send you a password reset email right now. Please, try again later or contact the site administration.";
            }
            public class Sms
            {
                public const string Verification = "We are sorry, we are unable to send you a verification SMS right now. Please, try again later or contact the site administration.";
            }
        }
        public class Request
        {
            public class Requirement
            {
                public const string Title = "Title";
                public const string ServiceType = "Service Type";
                public const string PickupAddress = "Source Address";
                public const string DeliveryAddress = "Delivery Address";
                public const string Cost = "Cost (1 to 10,000)";
                public const string Date = "Date";
                public const string Time = "Time";
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
