using EventVenueBookingManagement_System.Controllers;
using EventVenueBookingManagement_System.Models;

namespace EventVenueBookingManagement_System.Views;

public sealed class DashboardForm : Form
{
    private readonly User _user;
    private readonly VenueController _venueController = new();
    private readonly BookingController _bookingController = new();
    private readonly PaymentController _paymentController = new();
    private readonly DataGridView _venuesGrid = CreateGrid();
    private readonly DataGridView _bookingsGrid = CreateGrid();
    private readonly DataGridView _paymentsGrid = CreateGrid();
    private readonly TextBox _searchBox = new();
    private readonly bool _isAdministrator;

    public DashboardForm(User user)
    {
        _user = user;
        _isAdministrator = user.Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
        Text = "Event Venue Booking Management";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(950, 620);
        Size = new Size(1180, 760);
        Font = new Font("Segoe UI", 9.5F);
        BackColor = Color.FromArgb(245, 247, 250);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = BackColor };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 10, 12, 6), WrapContents = false, BackColor = Color.FromArgb(28, 43, 62) };
        header.Controls.Add(new Label
        {
            Text = $"EVENT VENUE  /  {user.FullName}  ·  {user.Role}", AutoSize = true, ForeColor = Color.White,
            Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 7, 24, 0)
        });
        var signOut = new Button { Text = "Sign out", AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(48, 65, 87), ForeColor = Color.White };
        signOut.FlatAppearance.BorderColor = Color.FromArgb(90, 110, 135);
        signOut.Click += (_, _) => Close();
        header.Controls.Add(signOut);
        root.Controls.Add(header, 0, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(18, 7), Font = new Font(Font, FontStyle.Bold) };
        tabs.TabPages.Add(BuildVenuesPage());
        tabs.TabPages.Add(BuildBookingsPage());
        tabs.TabPages.Add(BuildPaymentsPage());
        root.Controls.Add(tabs, 0, 1);
        Shown += (_, _) => RunSafely(RefreshAll);
    }

    private TabPage BuildVenuesPage()
    {
        var page = new TabPage("Venues");
        var layout = CreatePageLayout(page);
        var toolbar = CreateToolbar(layout);
        _searchBox.Width = 260;
        _searchBox.PlaceholderText = "Search name, location, details";
        _searchBox.TextChanged += (_, _) => RunSafely(RefreshVenues);
        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(CreateButton("Refresh", () => RunSafely(RefreshVenues)));

        if (_isAdministrator)
        {
            toolbar.Controls.Add(CreateButton("Add venue", AddVenue));
            toolbar.Controls.Add(CreateButton("Edit selected", EditVenue));
            toolbar.Controls.Add(CreateButton("Delete selected", DeleteVenue));
        }
        else
        {
            toolbar.Controls.Add(CreateButton("Book selected venue", CreateBooking));
        }

        layout.Controls.Add(_venuesGrid, 0, 1);
        return page;
    }

    private TabPage BuildBookingsPage()
    {
        var page = new TabPage("Bookings");
        var layout = CreatePageLayout(page);
        var toolbar = CreateToolbar(layout);
        toolbar.Controls.Add(CreateButton("Refresh", () => RunSafely(RefreshBookings)));
        if (_isAdministrator)
        {
            toolbar.Controls.Add(CreateButton("Approve selected", () => ChangeBookingStatus("Approved")));
            toolbar.Controls.Add(CreateButton("Reject selected", () => ChangeBookingStatus("Rejected")));
        }
        layout.Controls.Add(_bookingsGrid, 0, 1);
        return page;
    }

    private TabPage BuildPaymentsPage()
    {
        var page = new TabPage("Payments");
        var layout = CreatePageLayout(page);
        var toolbar = CreateToolbar(layout);
        toolbar.Controls.Add(CreateButton("Refresh", () => RunSafely(RefreshPayments)));
        if (_isAdministrator)
            toolbar.Controls.Add(CreateButton("Record payment", RecordPayment));
        layout.Controls.Add(_paymentsGrid, 0, 1);
        return page;
    }

    private static TableLayoutPanel CreatePageLayout(Control parent)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(8) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        parent.Controls.Add(layout);
        return layout;
    }

    private static FlowLayoutPanel CreateToolbar(TableLayoutPanel layout)
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        layout.Controls.Add(toolbar, 0, 0);
        return toolbar;
    }

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None,
        GridColor = Color.FromArgb(226, 232, 238),
        EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(232, 239, 244), ForeColor = Color.FromArgb(28, 43, 62),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold), Padding = new Padding(4)
        },
        DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(210, 232, 242), SelectionForeColor = Color.FromArgb(28, 43, 62), Padding = new Padding(3) },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(247, 250, 252) }
    };

    private static Button CreateButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, Margin = new Padding(4, 2, 4, 2),
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(35, 115, 145), ForeColor = Color.White, Padding = new Padding(5, 2, 5, 2)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        return button;
    }

    private void RefreshAll()
    {
        RefreshVenues();
        RefreshBookings();
        RefreshPayments();
    }

    private void RefreshVenues() => _venuesGrid.DataSource = _venueController.Search(_searchBox.Text, _isAdministrator).ToList();
    private void RefreshBookings() => _bookingsGrid.DataSource = _bookingController.GetBookings(_user).ToList();
    private void RefreshPayments() => _paymentsGrid.DataSource = _paymentController.GetPayments(_user).ToList();

    private void AddVenue()
    {
        using var dialog = new VenueDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
            RunSafely(() => { _venueController.Save(dialog.Venue); RefreshAll(); });
    }

    private void EditVenue()
    {
        if (Selected<Venue>(_venuesGrid) is not { } venue)
        {
            ShowMessage("Select a venue first.");
            return;
        }
        using var dialog = new VenueDialog(venue);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            RunSafely(() => { _venueController.Save(dialog.Venue); RefreshAll(); });
    }

    private void DeleteVenue()
    {
        if (Selected<Venue>(_venuesGrid) is not { } venue)
        {
            ShowMessage("Select a venue first.");
            return;
        }
        if (MessageBox.Show(this, $"Delete venue '{venue.Name}'?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        RunSafely(() =>
        {
            if (!_venueController.Delete(venue.Id))
                throw new InvalidOperationException("This venue cannot be deleted because it has booking records.");
            RefreshAll();
        });
    }

    private void CreateBooking()
    {
        if (Selected<Venue>(_venuesGrid) is not { } venue)
        {
            ShowMessage("Select a venue first.");
            return;
        }
        if (!venue.IsAvailable)
        {
            ShowMessage("This venue is not currently available for booking.");
            return;
        }
        using var dialog = new BookingDialog(venue);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            RunSafely(() =>
            {
                _bookingController.Create(_user, venue.Id, dialog.EventDate, dialog.StartTime, dialog.DurationDays, dialog.EventDetails);
                RefreshAll();
            });
    }

    private void ChangeBookingStatus(string status)
    {
        if (Selected<Booking>(_bookingsGrid) is not { } booking)
        {
            ShowMessage("Select a booking first.");
            return;
        }
        RunSafely(() => { _bookingController.SetStatus(booking.Id, status); RefreshAll(); });
    }

    private void RecordPayment()
    {
        if (Selected<PaymentSummary>(_paymentsGrid) is not { } payment)
        {
            ShowMessage("Select a payment record first.");
            return;
        }
        if (payment.Balance <= 0)
        {
            ShowMessage("This booking has no outstanding balance.");
            return;
        }
        using var dialog = new PaymentDialog(payment.Balance);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            RunSafely(() =>
            {
                _paymentController.Record(payment.BookingId, dialog.Amount, dialog.Method);
                RefreshAll();
            });
    }

    private static T? Selected<T>(DataGridView grid) where T : class => grid.CurrentRow?.DataBoundItem as T;

    private void RunSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Operation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowMessage(string message) => MessageBox.Show(this, message, "Event Venue Booking", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
