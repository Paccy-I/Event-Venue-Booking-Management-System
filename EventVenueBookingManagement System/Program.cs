namespace EventVenueBookingManagement_System
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new EventVenueBookingManagement_System.Views.LoginForm());
        }
    }
}