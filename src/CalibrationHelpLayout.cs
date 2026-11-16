using System;
using TMPro;
using ThunderRoad;

namespace BetterCalibrationUI
{
    // Preserve the native text frame and horizontal alignment; use its upper edge.
    internal sealed class CalibrationHelpLayout : IDisposable
    {
        private readonly TMP_Text text;
        private readonly VerticalAlignmentOptions originalAlignment;
        private readonly UIText localization;
        private readonly string originalGroup, originalText;

        internal CalibrationHelpLayout(TMP_Text text)
        {
            this.text = text;
            if (!text) return;
            originalAlignment = text.verticalAlignment;
            text.verticalAlignment = VerticalAlignmentOptions.Top;
            localization = text.GetComponent<UIText>();
            if (localization)
            {
                originalGroup = localization.textGroupId;
                originalText = localization.text;
                // UIText keeps font handling and refreshes this binding on language changes.
                localization.SetLocalizationIds("BetterCalibrationUIHelp", "{CalibrationInstructions}");
            }
        }

        public void Dispose()
        {
            if (text) text.verticalAlignment = originalAlignment;
            if (localization) localization.SetLocalizationIds(originalGroup, originalText);
        }
    }
}
