using EventVenueBookingManagement_System.Models;

namespace EventVenueBookingManagement_System.Views;

internal sealed class VenueDialog : Form
{
    private readonly TextBox _name = new();
    private readonly TextBox _location = new();
    private readonly NumericUpDown _capacity = new() { Minimum = 1, Maximum = 100000, Value = 50 };
    private readonly NumericUpDown _price = new() { Minimum = 0, Maximum = 99999999, DecimalPlaces = 2, ThousandsSeparator = true };
    private readonly TextBox _description = new() { Multiline = true, Height = 75, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _available = new() { Text = "Available for booking", Checked = true, AutoSize = true };
    private readonly int _venueId;

    public Venue Venue { get; private set; } = new();

    public VenueDialog(Venue? venue = null)
    {
        _venueId = venue?.Id ?? 0;
        Text = venue is null ? "Add venue" : "Edit venue";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 9.5F);
        BackColor = Color.FromArgb(245, 247, 250);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(14), ColumnCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        AddRow(layout, "Venue name", _name);
        AddRow(layout, "Location", _location);
        AddRow(layout, "Capacity", _capacity);
        AddRow(layout, "Price per day", _price);
        AddRow(layout, "Description", _description);
        layout.Controls.Add(_available, 1, layout.RowCount);
        layout.RowCount++;

        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var save = new Button { Text = "Save", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => SaveVenue();
        actions.Controls.Add(cancel);
        actions.Controls.Add(save);
        layout.Controls.Add(actions, 0, layout.RowCount);
        layout.SetColumnSpan(actions, 2);
        layout.RowCount++;
        Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;

        if (venue is not null)
        {
            _name.Text = venue.Name;
            _location.Text = venue.Location;
            _capacity.Value = Math.Clamp(venue.Capacity, (int)_capacity.Minimum, (int)_capacity.Maximum);
            _price.Value = Math.Clamp(venue.PricePerDay, _price.Minimum, _price.Maximum);
            _description.Text = venue.Description;
            _available.Checked = venue.IsAvailable;
        }
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control field)
    {
        var row = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) }, 0, row);
        field.Dock = DockStyle.Fill;
        layout.Controls.Add(field, 1, row);
    }

    private void SaveVenue()
    {
        if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_location.Text))
        {
            MessageBox.Show(this, "Venue name and location are required.", "Invalid venue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Venue = new Venue
        {
            Id = _venueId,
            Name = _name.Text.Trim(),
            Location = _location.Text.Trim(),
            Capacity = (int)_capacity.Value,
            PricePerDay = _price.Value,
            Description = _description.Text.Trim(),
            IsAvailable = _available.Checked
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class BookingDialog : Form
{
    private readonly DateTimePicker _eventDate = new() { MinDate = DateTime.Today, Value = DateTime.Today.AddDays(1), Format = DateTimePickerFormat.Long };
    private readonly DateTimePicker _startTime = new() { Format = DateTimePickerFormat.Time, ShowUpDown = true };
    private readonly NumericUpDown _durationDays = new() { Minimum = 1, Maximum = 30, Value = 1 };
    private readonly TextBox _details = new() { Multiline = true, Height = 80, ScrollBars = ScrollBars.Vertical };
    public DateTime EventDate => _eventDate.Value.Date;
    public TimeSpan StartTime => _startTime.Value.TimeOfDay;
    public int DurationDays => (int)_durationDays.Value;
    public string EventDetails => _details.Text;

    public BookingDialog(Venue venue)
    {
        Text = $"Book {venue.Name}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 9.5F);
        BackColor = Color.FromArgb(245, 247, 250);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(14), ColumnCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        layout.Controls.Add(new Label { Text = "Venue", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(new Label { Text = $"{venue.Name} — {venue.PricePerDay:0.00} per day", AutoSize = true, Anchor = AnchorStyles.Left }, 1, 0);
        layout.Controls.Add(new Label { Text = "First event date", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _eventDate.Dock = DockStyle.Fill;
        layout.Controls.Add(_eventDate, 1, 1);
        layout.Controls.Add(new Label { Text = "Start time", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        _startTime.Dock = DockStyle.Fill;
        layout.Controls.Add(_startTime, 1, 2);
        layout.Controls.Add(new Label { Text = "Number of days", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        _durationDays.Dock = DockStyle.Left;
        layout.Controls.Add(_durationDays, 1, 3);
        layout.Controls.Add(new Label { Text = "Event details", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
        _details.Dock = DockStyle.Fill;
        layout.Controls.Add(_details, 1, 4);

        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var submit = new Button { Text = "Submit booking", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        submit.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        actions.Controls.Add(cancel);
        actions.Controls.Add(submit);
        layout.Controls.Add(actions, 0, 5);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);
        AcceptButton = submit;
        CancelButton = cancel;
    }
}

internal sealed class PaymentDialog : Form
{
    private readonly NumericUpDown _amount;
    private readonly ComboBox _method = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    public decimal Amount => _amount.Value;
    public string Method => _method.SelectedItem?.ToString() ?? "";

    public PaymentDialog(decimal balance)
    {
        Text = "Record payment";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 9.5F);
        BackColor = Color.FromArgb(245, 247, 250);
        _amount = new NumericUpDown { Minimum = 0.01M, Maximum = Math.Max(0.01M, balance), DecimalPlaces = 2, ThousandsSeparator = true, Value = Math.Max(0.01M, balance), Dock = DockStyle.Fill };
        _method.Items.AddRange(["Cash", "Bank transfer", "Card", "Mobile money", "Other"]);
        _method.SelectedIndex = 0;
        _method.Dock = DockStyle.Fill;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(14), ColumnCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        layout.Controls.Add(new Label { Text = $"Balance: {balance:0.00}", AutoSize = true }, 0, 0);
        layout.SetColumnSpan(layout.Controls[0], 2);
        layout.Controls.Add(new Label { Text = "Amount", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_amount, 1, 1);
        layout.Controls.Add(new Label { Text = "Method", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        layout.Controls.Add(_method, 1, 2);

        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var save = new Button { Text = "Record", AutoSize = true, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        actions.Controls.Add(cancel);
        actions.Controls.Add(save);
        layout.Controls.Add(actions, 0, 3);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;
    }
}
