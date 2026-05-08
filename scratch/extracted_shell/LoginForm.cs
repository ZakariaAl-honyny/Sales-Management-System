    public class LoginForm : Form
    {
        private readonly AuthApiService _authApiService;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;

        public LoginForm(AuthApiService authApiService)
        {
            _authApiService = authApiService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = " ”ÃÌ· «·œŒÊ·";
            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
            BackColor = Color.White;

            var lblTitle = new Label { Text = "‰Ÿ«„ ≈œ«—… «·„»Ì⁄« ", Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 60 };
            
            txtUsername = new TextBox { Width = 250, PlaceholderText = "«”„ «·„” Œœ„" };
            txtPassword = new TextBox { Width = 250, PlaceholderText = "ﬂ·„… «·„—Ê—", UseSystemPasswordChar = true };
            
            btnLogin = new Button { Text = "œŒÊ·", Width = 250, Height = 40, BackColor = Color.FromArgb(33, 43, 54), ForeColor = Color.White };
            btnLogin.Click += async (_, _) => await PerformLoginAsync();
            AcceptButton = btnLogin;

            var flp = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(60, 20, 20, 20) };
            flp.Controls.AddRange(new Control[] { new Label { Text = "«”„ «·„” Œœ„:" }, txtUsername, new Label { Text = "ﬂ·„… «·„—Ê—:" }, txtPassword, new Label { Height = 10 }, btnLogin });

            Controls.Add(flp);
            Controls.Add(lblTitle);
        }

        private async Task PerformLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("«·—Ã«¡ ≈œŒ«· «”„ «·„” Œœ„ Êﬂ·„… «·„—Ê—."); return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                btnLogin.Enabled = false;

                var result = await _authApiService.LoginAsync(new LoginRequestDto
                {
                    UserName = txtUsername.Text,
                    Password = txtPassword.Text
                });

                if (result != null)
                {
                    TokenStore.Token = result.Token;
                    TokenStore.CurrentUserName = result.FullName;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Œÿ√", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { Cursor = Cursors.Default; btnLogin.Enabled = true; }
        }
    }
}
 ⁄œÌ· Program.cs ›Ì «·‹ Desktop · ‘€Ì· «·‹ Login √Ê·« Ê≈—›«ﬁ «·‹ Token
›Ì SalesSystem.Desktop/Program.cs:
C#
// 1. ≈‰‘«¡ Handler ·≈—›«ﬁ «· Êﬂ‰ „⁄ ﬂ· ÿ·»
