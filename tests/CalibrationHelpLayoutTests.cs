using System;
using BetterCalibrationUI;
using TMPro;
using ThunderRoad;

internal static class CalibrationHelpLayoutTests
{
    private static int checks;
    private static void Check(bool value) { if (!value) throw new Exception("Calibration help alignment regression"); checks++; }
    private static void Main()
    {
        var text = new TMP_Text { verticalAlignment = VerticalAlignmentOptions.Middle, text = "editable localized help" };
        var native = new UIText { target = text, textGroupId = "Default", text = "{TrackerCalibrationHelp}" };
        text.Localization = native;
        using (new CalibrationHelpLayout(text))
        {
            Check(text.verticalAlignment == VerticalAlignmentOptions.Top);
            Check(native.textGroupId == "BetterCalibrationUIHelp");
            Check(native.text == "{CalibrationInstructions}");
            Check(text.text == "English custom help");
            native.Language = "Japanese";
            native.Refresh();
            Check(text.text == "Japanese custom help");
        }
        Check(text.verticalAlignment == VerticalAlignmentOptions.Middle);
        Check(native.textGroupId == "Default");
        Check(native.text == "{TrackerCalibrationHelp}");
        Check(text.text == "Japanese native help from another mod");
        using (new CalibrationHelpLayout(text))
        {
            Check(text.verticalAlignment == VerticalAlignmentOptions.Top);
            Check(text.text == "Japanese custom help");
        }
        Check(text.verticalAlignment == VerticalAlignmentOptions.Middle);
        Check(text.text == "Japanese native help from another mod");
        new CalibrationHelpLayout(null).Dispose();
        var layout = new CalibrationHelpLayout(text);
        text.Destroyed = true;
        native.Destroyed = true;
        layout.Dispose();
        Check(text.verticalAlignment == VerticalAlignmentOptions.Top);
        var noLocalizer = new TMP_Text { text = "native fallback" };
        using (new CalibrationHelpLayout(noLocalizer)) Check(noLocalizer.text == "native fallback");
        Console.WriteLine("Calibration help layout: " + checks + " checks passed.");
    }
}
namespace TMPro
{
    public enum VerticalAlignmentOptions { Top, Middle }
    public class TMP_Text
    {
        public VerticalAlignmentOptions verticalAlignment;
        public string text;
        public bool Destroyed;
        public UIText Localization;
        public T GetComponent<T>() where T : class => Localization as T;
        public static implicit operator bool(TMP_Text value) => value != null && !value.Destroyed;
    }
}
namespace ThunderRoad
{
    public class UIText
    {
        public TMP_Text target;
        public string textGroupId, text;
        public string Language = "English";
        public bool Destroyed;
        public static implicit operator bool(UIText value) => value != null && !value.Destroyed;
        public void SetLocalizationIds(string groupId, string stringId)
        {
            textGroupId = groupId;
            text = stringId;
            Refresh();
        }
        public void Refresh() => target.text = Language + (textGroupId == "Default" ? " native help from another mod" : " custom help");
    }
}
