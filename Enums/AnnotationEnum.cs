namespace Bewegdeal.Enums
{
    public class AnnotationEnum
    {
        private const string _l = "de"; // en de

        public class General
        {
            public class Service
            {
                public static string Moving => _l == "de" ? "Umzug" : "Moving Service";
                public static string Removal => _l == "de" ? "Entrümpelung" : "Junk Removal";
                public static string Pickup => _l == "de" ? "Einkaufsabholung" : "Store Pickup";
                public static string Transport => _l == "de" ? "Fahrzeugtransport" : "Vehicle Transport";
            }
            public class Role
            {
                public static string Administrator => _l == "de" ? "Administrator" : "Administrator";
                public static string Customer => _l == "de" ? "Kunde" : "Customer";
                public static string Company => _l == "de" ? "Unternehmen" : "Company";
            }
            public class UserStatus
            {
                public static string Active => _l == "de" ? "Aktiv" : "Active";
                public static string Pending => _l == "de" ? "Ausstehend" : "Pending";
                public static string Blocked => _l == "de" ? "Gesperrt" : "Blocked";
                public static string Unverified => _l == "de" ? "Unbestätigt" : "Unverified";
            }
            public class RequestStatus
            {
                public static string Pending => _l == "de" ? "Ausstehend" : "Pending";
                public static string Negotiation => _l == "de" ? "Verhandlung" : "Negotiation";
                public static string Agreed => _l == "de" ? "Vereinbart" : "Agreed";
                public static string Resolved => _l == "de" ? "Gelöst" : "Resolved";
                public static string Cancelled => _l == "de" ? "Abgesagt" : "Cancelled";
                public static string Declined => _l == "de" ? "Abgelehnt" : "Declined";
            }
            public class ChatStatus
            {
                public static string Agreed => _l == "de" ? "Vereinbart" : "Agreed";
                public static string Ongoing => _l == "de" ? "Laufend" : "Ongoing";
                public static string Cancelled => _l == "de" ? "Abgesagt" : "Cancelled";
            }
            public class ChatFraud
            {
                public static string Safe => _l == "de" ? "Sicher" : "Safe";
                public static string Dubious => _l == "de" ? "Zweifelhaft" : "Dubious";
                public static string Resolved => _l == "de" ? "Gelöst" : "Resolved";
            }
            public class InvoiceStatus
            {
                public static string Cancelled => _l == "de" ? "Abgesagt" : "Cancelled";
                public static string Pending => _l == "de" ? "Ausstehend" : "Pending";
                public static string Paid => _l == "de" ? "Bezahlt" : "Paid";
            }
            public class RequestViewerFocus
            {
                public static string Mine => _l == "de" ? "Nur Meine" : "Only Mine";
                public static string Potential => _l == "de" ? "Potenzielle" : "Potential";
            }
        }
        public class Account
        {
            public class Login
            {
                public static string Credentials => _l == "de"
                    ? "Ungültige E-Mail-Adresse oder Passwort."
                    : "Invalid email or password.";
                public static string Blocked => _l == "de"
                    ? "Ihr Konto wurde gesperrt. Bitte kontaktieren Sie den Support."
                    : "Your account has been blocked. Please, contact support.";
                public static string Pending => _l == "de"
                    ? "Ihr Konto wartet auf Genehmigung. Bitte warten Sie auf die Bestätigung."
                    : "Your account is pending approval. Please, wait for confirmation.";
                public static string Unverified => _l == "de"
                    ? "Ihre E-Mail-Adresse wurde nicht bestätigt. Bitte bestätigen Sie Ihre E-Mail, um sich anzumelden."
                    : "Your email address is not verified. Please, verify your email to log in.";
            }
            public class ForgotPassword
            {
                public static string Success => _l == "de"
                    ? "Falls die E-Mail-Adresse registriert ist, erhalten Sie in Kürze einen Reset-Link."
                    : "If email is registered, you'll receive a reset link shortly.";
            }
            public class ResetPassword
            {
                public static string Expired => _l == "de"
                    ? "Ihr Link zum Zurücksetzen des Passworts ist abgelaufen oder ungültig. Bitte fordern Sie einen neuen an."
                    : "Your password reset link has expired or is invalid. Please request a new one.";
                public static string Success => _l == "de"
                    ? "Ihr Passwort wurde zurückgesetzt. Sie können sich jetzt anmelden."
                    : "Your password has been reset. You can now log in.";
            }
            public class VerifyEmail
            {
                public static string Expired => _l == "de"
                    ? "Die Bestätigungscodes sind abgelaufen. Bitte fordern Sie neue an."
                    : "Verification codes has expired. Please, request a new one.";
                public static string InvalidEmail => _l == "de"
                    ? "Ungültiger E-Mail-Bestätigungscode. Bitte geben Sie den richtigen Code ein."
                    : "Invalid email verification code. Please, reenter a correct one.";
                public static string InvalidMobile => _l == "de"
                    ? "Ungültiger Mobilnummer-Bestätigungscode. Bitte geben Sie den richtigen Code ein."
                    : "Invalid mobile verification code. Please, reenter a correct one.";
                public static string Success => _l == "de"
                    ? "Ihr Konto wurde bestätigt. Sie können sich jetzt anmelden."
                    : "Your account has been verified. You may sign in now.";
                public static string Resent => _l == "de"
                    ? "Neue Bestätigungscodes wurden an Ihre E-Mail-Adresse und Ihr Mobiltelefon gesendet."
                    : "New verification codes have been sent to your email and mobile.";
            }
            public class Register
            {
                public static string Exists => _l == "de"
                    ? "Ein Konto mit dieser E-Mail-Adresse oder Mobilnummer existiert bereits. Sie können sich stattdessen anmelden."
                    : "An account with this email address or mobile number already exists. You may sign in instead.";
            }
            public class Email
            {
                public static string Verification => _l == "de"
                    ? "Es tut uns leid, wir können Ihnen derzeit keine Bestätigungs-E-Mail senden. Bitte versuchen Sie es später erneut oder kontaktieren Sie die Site-Administration."
                    : "We are sorry, we are unable to send you a verification email right now. Please, try again later or contact the site administration.";
                public static string Reset => _l == "de"
                    ? "Es tut uns leid, wir können Ihnen derzeit keine E-Mail zum Zurücksetzen des Passworts senden. Bitte versuchen Sie es später erneut oder kontaktieren Sie die Site-Administration."
                    : "We are sorry, we are unable to send you a password reset email right now. Please, try again later or contact the site administration.";
            }
            public class Sms
            {
                public static string Verification => _l == "de"
                    ? "Es tut uns leid, wir können Ihnen derzeit keine Bestätigungs-SMS senden. Bitte versuchen Sie es später erneut oder kontaktieren Sie die Site-Administration."
                    : "We are sorry, we are unable to send you a verification sms right now. Please, try again later or contact the site administration.";
            }
        }
        public class Request
        {
            public class Requirement
            {
                public static string Title => _l == "de" ? "Titel" : "Title";
                public static string ServiceType => _l == "de" ? "Dienstleistungstyp" : "Service Type";
                public static string PickupAddress => _l == "de" ? "Abholadresse" : "Source Address";
                public static string DeliveryAddress => _l == "de" ? "Lieferadresse" : "Delivery Address";
                public static string Cost => _l == "de" ? "Kosten (1 bis 10.000)" : "Cost (1 to 10,000)";
                public static string Date => _l == "de" ? "Datum" : "Date";
                public static string Time => _l == "de" ? "Uhrzeit" : "Time";
                public static string VehicleType => _l == "de" ? "Fahrzeugtyp" : "Vehicle Type";
                public static string VehicleCondition => _l == "de" ? "Fahrzeugzustand" : "Vehicle Condition";
                public static string Error => _l == "de" ? "Das Feld {0} ist erforderlich." : "{0} field is required.";
            }
            public class Media
            {
                public static string ImageMinCount => _l == "de" ? "Das Bildfeld ist erforderlich." : "Image field is required.";
                public static string ImageMaxCount => _l == "de" ? "Maximal {0} Bilder erlaubt." : "Maximum {0} images allowed.";
                public static string VideoMaxCount => _l == "de" ? "Maximal {0} Videos erlaubt." : "Maximum {0} videos allowed.";
            }
        }
    }
}
