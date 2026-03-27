namespace WorkBase.Modules.TimeTracking.Application.Contracts;

public sealed class AnomalyDetectionSettings
{
    public TimeSpan LateArrivalThreshold { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan ExcessiveShiftThreshold { get; set; } = TimeSpan.FromHours(12);
    public bool DetectMissingClockOut { get; set; } = true;
    public bool DetectLateArrival { get; set; } = true;
    public bool DetectDoubleClockIn { get; set; } = true;
    public bool DetectExcessiveShift { get; set; } = true;
    public bool DetectMissingClockIn { get; set; } = true;
}
