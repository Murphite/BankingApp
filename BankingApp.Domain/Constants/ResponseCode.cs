

namespace BankingApp.Domain.Constants
{
    public class ResponseCode
    {
        public const string SUCCESSFUL = "00";
        public static string DuplicateRequest = "86";
        public static string BadRequest = "88";
        public static string Error = "400";
        public static string NOTFOUND = "404";
        public static string CONFLICT = "409";
        public const string SERVER_ERROR = "500";
        public const string FAILED = "99";
    }

}
