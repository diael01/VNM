using Infrastructure.Enums;

namespace Infrastructure.Options;
    public class ConsumptionPollingOptions
    {
        public string Protocol { get; set; } = "http";
        public string HttpEndpoint { get; set; } = string.Empty;
        public string Source { get; set; } = "Provider";
        public bool Enabled { get; set; } = true;

        // Add more options as needed for smart meter, etc.
    }

    public class ConsumptionSimulatorOptions
    {
        public decimal MinConsumption { get; set; } = 0m;
        public decimal MaxConsumption { get; set; } = 10000.1111m;

        public int MinInverterId { get; set; } = 1;
        public int MaxInverterId  { get; set; } = 10;
    }

	public class DailyBalanceComputationOptions
	{
		public bool Enabled { get; set; } = true;	
		 public int IntervalMinutes { get; set; } = 1;
	}

    public class TransferWorkflowOptions
    {
        public bool Enabled { get; set; } = true;
        public int PollIntervalSeconds { get; set; } = 30;

    }


    public class SettlementModeOptions
    {
        public const string SectionName = "SettlementMode";
        public string CurrentMode { get; set; } = "Money";
    }

    public class InverterPollingOptions
    {
        public string Source { get; set; } = "InverterSimulator";

        // New fields for protocol selection
        public string Protocol { get; set; } = "Http";  // "Http" or "Tcp"
        public string HttpEndpoint { get; set; } = "http://localhost:5000/inverter/data";
        public string TcpHost { get; set; } = "127.0.0.1";
        public int TcpPort { get; set; } = 15000;

        public bool Enabled { get; set; } = true;
    }


    public class InverterSimulatorOptions
    {
        public decimal MinPower { get; set; } = 0;
        public decimal MaxPower { get; set; } = 5000.55m;

        public decimal MinVoltage { get; set; } = 200.22m;
        public decimal MaxVoltage { get; set; } = 250.55m;

        public decimal MinCurrent { get; set; } = 0m;
        public decimal MaxCurrent { get; set; } = 20.22m;

        public int MinInverterId { get; set; } = 1;
        public int MaxInverterId { get; set; } = 10;
    }

    public class MeteringOptions
    {
        public int ReadingIntervalMinutes { get; set; } = 1;
    }

    public class ExecutionPolicyOptions
    {
        public int ManualReviewRequired {get; set;}
        public int AutoExecute {get; set;}
        public int AutoExecuteAutoSettle {get; set;}

    }


public class TransferExecutionSimulatorOptions
{
    public string BaseUrl { get; set; } = string.Empty;
}

