# Event Venue Booking Management System

A C# WinForms desktop application using MVC-style controllers, views, and data repositories with MySQL (XAMPP) storage.

## Requirements

- Windows with the .NET 10 SDK
- Visual Studio with the .NET desktop development workload
- XAMPP with MySQL/MariaDB running

## Database setup

1. Start MySQL in the XAMPP Control Panel.
2. Open phpMyAdmin (`http://localhost/phpmyadmin`), select **Import**, and import `EventVenueBookingManagement System/Database/schema.sql`.
3. The default local connection is `Server=localhost;Port=3306;Database=event_venue_booking;User ID=root;Password=;`. For a different account/password, set the `EVENT_VENUE_DB` environment variable to a MySQL connection string before starting the application. Do not commit credentials.
4. Run the application and register an account.
5. To make that account an administrator, execute this in phpMyAdmin's SQL tab, replacing the email:

   ```sql
   UPDATE users SET role = 'Administrator' WHERE email = 'your-email@example.com';
   ```

   Sign out and sign in again to load the updated role.

## Run

Open `EventVenueBookingManagement System.slnx` in Visual Studio, restore NuGet packages, and run the project. Or run:

```powershell
dotnet run --project "EventVenueBookingManagement System/EventVenueBookingManagement System.csproj"
```

Customers can browse/search available venues, submit bookings with a start time and duration (1–30 days), and view booking/payment status. Prices are calculated as the venue's daily rate multiplied by the number of booked days. Administrators can manage venues, approve/reject bookings, and record payments. Booking dates cannot overlap for the same venue. When an approved booking is fully paid, the venue is marked unavailable and hidden from customers.

### Upgrade an existing database

If you imported the earlier schema already, select the `event_venue_booking` database in phpMyAdmin, then use **Import** to run `EventVenueBookingManagement System/Database/upgrade_booking_duration.sql` before running the updated application. This adds the start-time and duration columns required by the new booking form.

## MVC layout

- `Models/Entities.cs`: application data models
- `Views/`: WinForms user interface
- `Controllers/AppControllers.cs`: application rules and validation
- `Data/`: MySQL access and repositories
- `Database/schema.sql`: schema and optional sample venue

