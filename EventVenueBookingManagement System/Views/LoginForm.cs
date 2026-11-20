using EventVenueBookingManagement_System.Controllers;
using EventVenueBookingManagement_System.Models;

namespace EventVenueBookingManagement_System.Views;

public sealed class LoginForm : Form
{
    private readonly AuthController _auth = new();
    private readonly TextBox _loginEmail = new();
    private readonly TextBox _loginPassword = new();
    private readonly TextBox _registerName = new();
    private readonly TextBox _registerEmail = new();
    private readonly TextBox _registerPassword = new();

    public LoginForm()
    {
        Text = "Event Venue Booking - Sign in";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 460);
        Size = new Size(580, 500);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 6), Font = new Font(Font, FontStyle.Bold) };
        var signIn = new TabPage("Sign in");
        var register = new TabPage("Create account");
        tabs.TabPages.Add(signIn);
        tabs.TabPages.Add(register);
        Controls.Add(tabs);

        BuildSignIn(signIn);
        BuildRegistration(register);
    }

    private void BuildSignIn(Control page)
    {
        var panel = CreateFormPanel(page);
        panel.Controls.Add(new Label { Text = "Welcome back", AutoSize = true, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(28, 43, 62), Margin = new Padding(3, 8, 3, 16) });
        AddField(panel, "Email", _loginEmail);
        AddField(panel, "Password", _loginPassword, password: true);
        var button = new Button { Text = "Sign in", AutoSize = true, Padding = new Padding(12, 6, 12, 6), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(35, 115, 145), ForeColor = Color.White };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => SignIn();
        panel.Controls.Add(button);
        AcceptButton = button;
    }

    private void BuildRegistration(Control page)
    {
        var panel = CreateFormPanel(page);
        panel.Controls.Add(new Label { Text = "Create your account", AutoSize = true, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(28, 43, 62), Margin = new Padding(3, 8, 3, 16) });
        AddField(panel, "Full name", _registerName);
        AddField(panel, "Email", _registerEmail);
        AddField(panel, "Password (at least 8 characters)", _registerPassword, password: true);
        var button = new Button { Text = "Create customer account", AutoSize = true, Padding = new Padding(12, 6, 12, 6), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(35, 115, 145), ForeColor = Color.White };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => Register();
        panel.Controls.Add(button);
    }

    private static FlowLayoutPanel CreateFormPanel(Control parent)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(28),
            AutoScroll = true
        };
        parent.Controls.Add(panel);
        return panel;
    }

    private static void AddField(FlowLayoutPanel panel, string label, TextBox input, bool password = false)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 10, 3, 2) });
        input.Width = 430;
        input.MaxLength = 190;
        if (password)
            input.UseSystemPasswordChar = true;
        panel.Controls.Add(input);
    }

    private void SignIn()
    {
        try
        {
            var user = _auth.Login(_loginEmail.Text, _loginPassword.Text);
            if (user is null)
            {
                MessageBox.Show(this, "The email or password is incorrect.", "Sign in failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            OpenDashboard(user);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void Register()
    {
        try
        {
            _auth.Register(_registerName.Text, _registerEmail.Text, _registerPassword.Text);
            _registerName.Clear();
            _registerEmail.Clear();
            _registerPassword.Clear();
            _loginEmail.Clear();
            _loginPassword.Clear();
            MessageBox.Show(this, "Your account is ready. Sign in to continue.", "Account created", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void OpenDashboard(User user)
    {
        Hide();
        using (var dashboard = new DashboardForm(user))
            dashboard.ShowDialog(this);
        _loginEmail.Clear();
        _loginPassword.Clear();
        Show();
    }

    private void ShowError(Exception exception) => MessageBox.Show(
        this,
        $"The operation could not be completed. Check the database connection and input, then try again.\n\nDetails: {exception.Message}",
        "Event Venue Booking", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
