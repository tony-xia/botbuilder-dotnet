﻿using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.Bot.Builder.TestBot.Middleware.Telemetry
{
    internal static class TelemetryHelper
    {
        public static bool HasTranscript(ITelemetry item) =>
            !(item is EventTelemetry evt) ? false : evt.Properties.ContainsKey(TelemetryConstants.Transcript);

        public static string GetTranscript(ITelemetry item)
        {
            var evt = (EventTelemetry)item;
            var transItem = evt.Properties[TelemetryConstants.Transcript];
            return transItem;
        }

        public static ITelemetry RemoveTranscriptForEvent(ITelemetry item)
        {
            var evt = (EventTelemetry)item;
            evt.Properties.Remove(TelemetryConstants.Transcript);
            return evt;
        }
    }
}