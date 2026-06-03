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
                public const string Unverified = "Your email address is not verified. Please, verify your email to log in.";
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
                public const string Expired = "Verification codes has expired. Please, request a new one.";
                public const string InvalidEmail = "Invalid email verification code. Please, reenter a correct one.";
                public const string InvalidMobile = "Invalid mobile verification code. Please, reenter a correct one.";
                public const string Success = "Your account has been verified. You may sign in now.";
                public const string Resent = "New verification codes have been sent to your email and mobile.";
            }
            public class Register
            {
                public const string Exists = "An account with this email address or mobile number already exists. You may sign in instead.";
            }
            public class Email
            {
                public const string Verification = "We are sorry, we are unable to send you a verification email right now. Please, try again later or contact the site administration.";
                public const string Reset = "We are sorry, we are unable to send you a password reset email right now. Please, try again later or contact the site administration.";
            }
            public class Sms
            {
                public const string Verification = "We are sorry, we are unable to send you a verification sms right now. Please, try again later or contact the site administration.";
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
