namespace RaceDay.MVC.Services
{
    // Keeps each user's API session cookie in their own MVC session
    public class ApiSessionHandler : DelegatingHandler
    {
        private const string SessionKey = "ApiCookie";
        private readonly IHttpContextAccessor _accessor;

        public ApiSessionHandler(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var session = _accessor.HttpContext?.Session;
            var stored = session?.GetString(SessionKey);
            if (!string.IsNullOrEmpty(stored))
            {
                request.Headers.TryAddWithoutValidation("Cookie", stored);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (session != null && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                var jar = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(stored))
                {
                    foreach (var part in stored.Split("; ", StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kv = part.Split('=', 2);
                        if (kv.Length == 2) jar[kv[0]] = kv[1];
                    }
                }
                foreach (var header in setCookies)
                {
                    var kv = header.Split(';')[0].Split('=', 2);
                    if (kv.Length == 2) jar[kv[0]] = kv[1];
                }
                session.SetString(SessionKey, string.Join("; ", jar.Select(p => p.Key + "=" + p.Value)));
            }

            return response;
        }
    }
}
