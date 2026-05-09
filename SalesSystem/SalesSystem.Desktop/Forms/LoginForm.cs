using SalesSystem.Desktop.Models;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;

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
            var result = await _authApiService.LoginAsync(new LoginRequest(username, password));

            if (result.IsSuccess && result.Value != null)
            {
                _sessionService.SignIn(new UserSession { UserId = result.Value.UserId, UserName = result.Value.UserName, FullName = result.Value.FullName, Role = (UserRole)result.Value.Role, Token = result.Value.Token });
                
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








