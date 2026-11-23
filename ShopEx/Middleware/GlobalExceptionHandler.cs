using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ShopEx.Middleware
{
    /// <summary>
    /// معالج موحد لأي Exception لم يُعالج داخل الـ Controller.
    /// يرجع دائماً JSON بالشكل { message } (نفس الشكل الذي يقرأه الـ Frontend)،
    /// ولا يكشف رسائل الأخطاء الداخلية (قاعدة البيانات / EF / الـ Framework) للمستخدم.
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private const string UnexpectedMessage = "حدث خطأ غير متوقع في الخادم. حاول مرة أخرى، ولو تكررت المشكلة تواصل مع الدعم.";

        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // العميل قفل الاتصال أو ألغى الطلب: لا يوجد من يستقبل الرد
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                httpContext.Response.StatusCode = 499; // Client Closed Request
                return true;
            }

            var (status, message) = Map(exception);

            if (status >= StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            else if (exception is DbUpdateException)
                _logger.LogWarning(exception, "Database update rejected on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

            httpContext.Response.StatusCode = status;

            var body = new Dictionary<string, object?> { ["message"] = message };
            if (_env.IsDevelopment() && status >= StatusCodes.Status500InternalServerError)
                body["detail"] = exception.ToString(); // للمطور فقط، لا يظهر في الإنتاج

            await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
            return true;
        }

        private static (int Status, string Message) Map(Exception exception)
        {
            switch (exception)
            {
                // ---- أخطاء قاعدة البيانات ----
                case DbUpdateConcurrencyException:
                    return (StatusCodes.Status409Conflict,
                        "البيانات اتعدلت من مستخدم آخر في نفس الوقت. حدّث الصفحة وحاول مرة أخرى.");

                case DbUpdateException { InnerException: SqlException sql } when sql.Number is 2601 or 2627:
                    return (StatusCodes.Status409Conflict,
                        "يوجد سجل بنفس البيانات بالفعل (ربما تم الحفظ من مستخدم آخر في نفس اللحظة). حاول مرة أخرى.");

                case DbUpdateException { InnerException: SqlException sql } when sql.Number == 547:
                    return (StatusCodes.Status409Conflict,
                        "لا يمكن تنفيذ العملية لأنها تخالف قيود البيانات (مثلاً كمية غير كافية أو سجل مرتبط ببيانات أخرى).");

                // ---- أخطاء منطق العمل (رسائلها مكتوبة للمستخدم داخل طبقة الـ Application) ----
                case ArgumentNullException when IsOurs(exception):
                    return (StatusCodes.Status400BadRequest, "بيانات الطلب مطلوبة.");

                case ArgumentException arg when IsOurs(exception):
                    return (StatusCodes.Status400BadRequest, WithoutParamName(arg));

                case KeyNotFoundException when IsOurs(exception):
                    return (StatusCodes.Status404NotFound, exception.Message);

                case InvalidOperationException when IsOurs(exception):
                    return (StatusCodes.Status400BadRequest, exception.Message);

                case UnauthorizedAccessException when IsOurs(exception):
                    return (StatusCodes.Status403Forbidden, exception.Message);

                default:
                    return (StatusCodes.Status500InternalServerError, UnexpectedMessage);
            }
        }

        /// <summary>يحذف " (Parameter 'X')" التي يضيفها .NET لرسالة ArgumentException.</summary>
        private static string WithoutParamName(ArgumentException ex)
        {
            if (string.IsNullOrEmpty(ex.ParamName))
                return ex.Message;

            var suffix = $" (Parameter '{ex.ParamName}')";
            return ex.Message.EndsWith(suffix, StringComparison.Ordinal) ? ex.Message[..^suffix.Length] : ex.Message;
        }

        /// <summary>
        /// الـ Exception صادر من كودنا (ShopEx.*) وليس من EF أو الـ Framework،
        /// وبالتالي رسالته موجهة للمستخدم ومسموح بعرضها.
        /// </summary>
        private static bool IsOurs(Exception exception)
            => exception.Source?.StartsWith("ShopEx", StringComparison.Ordinal) == true;
    }
}
