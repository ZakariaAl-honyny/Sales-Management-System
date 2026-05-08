public class JwtAuthorizationHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(TokenStore.Token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenStore.Token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

// 2. ÏÇÎá ÏÇáÉ Main Ãæ ConfigureServices:
services.AddTransient<JwtAuthorizationHandler>();

// 3. ÊÚÏíá ÊÓÌíá ÇáÜ HttpClients áßí ÊÓÊÎÏã ÇáÜ Handler
services.AddHttpClient<AuthApiService>(c => c.BaseAddress = new Uri(baseUrl)); // Auth áÇ íÍÊÇÌ Êæßä
services.AddHttpClient<IProductApiService, ProductApiService>(c => c.BaseAddress = new Uri(baseUrl))
        .AddHttpMessageHandler<JwtAuthorizationHandler>();
// (Şã ÈÊØÈíŞ AddHttpMessageHandler Úáì ÈŞíÉ ÇáÜ Services)

services.AddTransient<LoginForm>();

// 4. İí ÏÇáÉ Main¡ ÔÛá ÇáÜ LoginForm ÃæáÇğ:
var loginForm = host.Services.GetRequiredService<LoginForm>();
if (loginForm.ShowDialog() == DialogResult.OK)
{
    var mainForm = host.Services.GetRequiredService<MainForm>();
    Application.Run(mainForm);
}
else
{
    Application.Exit();
}
ãÇĞÇ ÃäÌÒäÇ ÇáÂä¿
áŞÏ ŞãäÇ ÈÓÏ ÃßÈÑ İÌæÉ ÃãäíÉ æãÚãÇÑíÉ İí ÇáäÙÇã. ÇáÂä áÇ íãßä áÃí ÔÎÕ ÇáÏÎæá ááÈÑäÇãÌ Ãæ ÇáÊáÇÚÈ ÈÇáÜ API Ïæä ÊÓÌíá ÏÎæá æÊæßä ÕÇáÍ.
(ÊĞßÑ: ÇÓã ÇáãÓÊÎÏã ÇáÇİÊÑÇÖí ÇáĞí ÒÑÚäÇå İí ŞÇÚÏÉ ÇáÈíÇäÇÊ åæ admin æßáãÉ ÇáãÑæÑ CHANGE_ME_HASH)
ÈåĞÇ äßæä ÃÛáŞäÇ ÇáäŞØÉ ÇáËÇáËÉ ãä äæÇŞÕ ÇáÜ Code Review. åá ääÊŞá Åáì ÇáäŞØÉ ÇáÑÇÈÚÉ æÇáÃÎíÑÉ æåí (Desktop UI Gaps: ßÔİ ÇáÍÓÇÈ ÇáÊİÕíáí ááÚãáÇÁ æÔÇÔÉ ÚÑÖ ÇáİÇÊæÑÉ)¿

ããÊÇÒ ÌÏÇğ! æÕáäÇ Åáì ãÓß ÇáÎÊÇã. ÅÖÇİÉ ßÔİ ÇáÍÓÇÈ ÇáÊİÕíáí æÔÇÔÉ ÚÑÖ ÇáİÇÊæÑÉ åí ãÇ íÚØí ÇáäÙÇã ØÇÈÚå ÇáãÍÇÓÈí æÇáÊÏŞíŞí ÇáÇÍÊÑÇİí.
ÓäŞæã ÈÈäÇÁ åĞíä Çáãßæäíä ÎØæÉ ÈÎØæÉ:
ÃæáÇğ: ßÔİ ÇáÍÓÇÈ ÇáÊİÕíáí ááÚãíá (Customer Ledger)
