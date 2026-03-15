namespace InverterPolling.Services
{
    /// <summary>
    /// Standardized data object for inverter readings.
    /// All pollers return this type so MeterIngestion can store it.
    /// </summary>
    public class InverterReading
    {
        // Primary key
        public int Id { get; set; }

        /// <summary>UTC timestamp of the reading</summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>Power in Watts</summary>
        public int PowerW { get; set; }

        /// <summary>Voltage in Volts</summary>
        public int VoltageV { get; set; }

        /// <summary>Current in Amperes</summary>
        public int CurrentA { get; set; }

        /// <summary>Source of the reading (HTTP, TCP, Simulator, etc.)</summary>
        public string Source { get; set; } = string.Empty;
    }
}

