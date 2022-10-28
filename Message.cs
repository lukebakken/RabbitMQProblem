namespace Vendeq.Api.Common.Messages
{
    public class AuditEvent
    {
        public Guid? CustomerId { get; set; }
        public Guid? UserId { get; set; }
        public string EventType { get; set; } = default!;
        public Dictionary<string, object>? EventParams { get; set; }
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public string? ClientIP { get; set; }
        public string? TargetId { get; set; }
        public string? TargetName { get; set; }
        public string? TargetType { get; set; }
        public string? ChildTargetId { get; set; }
        public string? ChildTargetName { get; set; }
        public string? ChildTargetType { get; set; }
        public object? Details { get; set; }
    }

    public record MTMessage
    (
        string MessageId,
        string ConversationId,
        string SourceAddress,
        string DestinationAddress,
        string[] MessageType,
        object Message,
        DateTimeOffset SentTime,
        Dictionary<string, string> Headers,
        MTHost Host
    );

    public record MTHost(
        string MachineName,
        string ProcessName,
        int ProcessId,
        string Assembly,
        string AssemblyVersion,
        string FrameworkVersion,
        string MassTransitVersion,
        string OperatingSystemVersion
    );
}
