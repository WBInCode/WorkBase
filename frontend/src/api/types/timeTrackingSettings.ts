/** Mirrors AnomalyDetectionSettings (backend). TimeSpan fields are serialized as "hh:mm:ss". */
export interface AnomalyDetectionSettingsDto {
  lateArrivalThreshold: string;
  excessiveShiftThreshold: string;
  detectMissingClockOut: boolean;
  detectLateArrival: boolean;
  detectDoubleClockIn: boolean;
  detectExcessiveShift: boolean;
  detectMissingClockIn: boolean;
  detectWorkOnDayOff: boolean;
}
