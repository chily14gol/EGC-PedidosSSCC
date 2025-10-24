using System;

namespace PedidosSSCC.ViewModels
{
    public class LogEntryViewModel
    {
        public string Level { get; set; }
        public DateTime? Timestamp { get; set; }
        public string RawTimestamp { get; set; }
        public string Url { get; set; }
        public string Message { get; set; }
        public string FullMessage { get; set; }
        public string OriginalLine { get; set; }

        public string DisplayTimestamp
        {
            get
            {
                return Timestamp.HasValue
                    ? Timestamp.Value.ToString("dd/MM/yyyy HH:mm:ss")
                    : RawTimestamp;
            }
        }

        public string IconClass
        {
            get
            {
                switch (Level)
                {
                    case "ERROR":
                        return "bi-bug-fill text-danger";
                    case "WARN":
                        return "bi-exclamation-circle-fill text-warning";
                    case "INFO":
                        return "bi-info-circle-fill text-primary";
                    case "DEBUG":
                        return "bi-gear-fill text-muted";
                    default:
                        return "bi-question-circle text-secondary";
                }
            }
        }
    }
}
