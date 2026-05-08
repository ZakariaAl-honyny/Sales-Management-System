    public abstract class ApiServiceBase
    {
        protected readonly HttpClient HttpClient;

        protected ApiServiceBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        // ÏÇáÉ ÊÛáíİ (Wrapper) áÍãÇíÉ ßá ÇáØáÈÇÊ ãä ÇäåíÇÑ ÇáÔÈßÉ
        protected async Task<T?> ExecuteSafeAsync<T>(Func<Task<HttpResponseMessage>> apiCall)
        {
            try
            {
                var response = await apiCall();
                
                // ÅĞÇ ßÇä ÇáÑÏ ÎØÃ ãä ÇáÜ API äİÓå (ãËá: ßãíÉ ÛíÑ ßÇİíÉ¡ ÈíÇäÇÊ äÇŞÕÉ)
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadFromJsonAsync<ApiResponseDto<object>>();
                    MessageBox.Show(error?.Message ?? "ÍÏË ÎØÃ İí ãÚÇáÌÉ ÇáÚãáíÉ.", "ÎØÃ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return default;
                }

                // ÅĞÇ äÌÍ ÇáØáÈ
                var result = await response.Content.ReadFromJsonAsync<ApiResponseDto<T>>();
                return result != null ? result.Data : default;
            }
            catch (HttpRequestException)
            {
                // ÇáÊŞÇØ ÃÎØÇÁ ÇäŞØÇÚ ÇáÓíÑİÑ Ãæ ÇáÔÈßÉ
                MessageBox.Show("ÊÚĞÑ ÇáÇÊÕÇá ÈÇáÎÇÏã ÇáãÑßÒí (API). íÑÌì ÇáÊÍŞŞ ãä ÇÊÕÇá ÇáÔÈßÉ Ãæ ÇáÊÃßÏ ãä ÊÔÛíá ÇáÓíÑİÑ.", "ÇäŞØÇÚ ÇáÇÊÕÇá", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return default;
            }
            catch (TaskCanceledException)
            {
                // ÇáÊŞÇØ ÃÎØÇÁ ÈØÁ æÖÚİ ÇáÅäÊÑäÊ (Timeout)
                MessageBox.Show("ÇäÊåì æŞÊ ÇáÇÊÕÇá ÈÇáÎÇÏã. ÇáÔÈßÉ ÈØíÆÉ ÌÏÇğ¡ ÍÇæá ãÑÉ ÃÎÑì.", "ÖÚİ ÇáÇÊÕÇá", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return default;
            }
            catch (Exception ex)
            {
                // Ãí ÎØÃ ÂÎÑ ÛíÑ ãÊæŞÚ
                MessageBox.Show($"ÍÏË ÎØÃ ÛíÑ ãÊæŞÚ İí ÇáäÙÇã: {ex.Message}", "ÎØÃ ÇáäÙÇã", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return default;
            }
        }

        // íãßäß ÇáÂä ÇÓÊÎÏÇã ExecuteSafeAsync ÏÇÎá ÇáÜ Services ÇáÃÎÑì
        // ãËÇá: return await ExecuteSafeAsync<List<ProductDto>>(() => HttpClient.GetAsync("api/products"));
    }
}
2. ÊÓÑíÚ ÇáÅÏÎÇá ÈÇáßíÈæÑÏ (Enter to Tab)
İí ÔÇÔÇÊ ÇáãÈíÚÇÊ Ãæ ÇáÅÖÇİÉ¡ ÇáßÇÔíÑ íßÊÈ ÇáÑŞã¡ Ëã íÖÛØ (Enter). İí ÇáÜ WinForms ÇáÇİÊÑÇÖí¡ ÒÑ Enter ŞÏ áÇ íİÚá ÔíÆÇğ Ãæ íÖÛØ ÒÑ ÇáÍİÙ İæÑÇğ. ÓäŞæã ÈßÊÇÈÉ ÃÏÇÉ ãÓÇÚÏÉ ÊÌÚá ÒÑ (Enter) íäŞá ÇáãÓÊÎÏã ááÍŞá ÇáÊÇáí (ãËá ÒÑ Tab ÊãÇãÇğ)¡ ããÇ íÓÑÚ ÇáÚãá ÈÔßá ÑåíÈ.
ÃäÔÆ ãÌáÏÇğ ÈÇÓã SalesSystem.Desktop/Helpers æÃÖİ İíå ßáÇÓ FormExtensions.cs:
