namespace ClimateMonitor.Exceptions
{
    public class VersionException: Exception
    {
        public VersionException()
            :base(message: "The firmware value does not match semantic versioning format.")
        {
            
        }
    }
}
