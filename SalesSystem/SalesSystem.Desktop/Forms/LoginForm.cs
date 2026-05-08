using Microsoft.Extensions.DependencyInjection;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Forms;

public partial class LoginForm : Form
{
    private readonly IAuthApiService _authApiService;
    private readonly ISessionService _sessionService;
    private readonly IServiceProvider _serviceProvider;

    public LoginForm(
        IAuthApiService authApiService, 
        ISessionService sessionService,
        IServiceProvider serviceProvider)
    {
        _authApiService = authApiService;
        _sessionService = sessionService;
        _serviceProvider = serviceProvider;
        InitializeComponent();
    }

    private async void btnLogin_Click(object sender, EventArgs e)
    {
        await PerformLoginAsync();
    }

    private async Task PerformLoginAsync()
    {
        string username = txtUserName.Text.Trim();
        string password = txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("يرجى إدخال اسم المستخدم وكلمة المرور");
            return;
        }

        try
        {
            SetLoading(true);
            var result = await _authApiService.LoginAsync(username, password);

            if (result.IsSuccess && result.Value != null)
            {
                _sessionService.SignIn(result.Value);
                
                var mainForm = _serviceProvider.GetRequiredService<MainForm>();
                mainForm.Show();
                this.Hide();
            }
            else
            {
                ShowError(result.Error ?? "فشل تسجيل الدخول");
            }
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void SetLoading(bool isLoading)
    {
        btnLogin.Enabled = !isLoading;
        btnLogin.Text = isLoading ? "جاري الدخول..." : "دخول";
        txtUserName.Enabled = !isLoading;
        txtPassword.Enabled = !isLoading;
        lblError.Visible = false;
    }

    private void ShowError(string message)
    {
        lblError.Text = message;
        lblError.Visible = true;
    }
}
