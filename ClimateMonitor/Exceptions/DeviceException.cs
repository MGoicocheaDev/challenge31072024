namespace ClimateMonitor.Exceptions
{
    public class DeviceException: Exception
    {
        public DeviceException()
            :base(message: "Device secret is not within the valid range.")
        {
            
        }
    }
}
