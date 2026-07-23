using System.Collections.Generic;

namespace WPZ0325.EasyConfig.Demo
{
    [System.Serializable]
    public class UIPosition
    {
        public float X;
        public float Y;
    }

    [System.Serializable]
    public class UIButtonConfig
    {
        public string Label;
        public int IconIndex;
        public bool Enabled;
    }

    [System.Serializable]
    public class UILayoutConfig : IEasyConfigBase<UILayoutConfig>
    {
        public int FontSize;

        public string ThemeColor;

        public UIPosition MainMenuPos;

        public List<UIButtonConfig> HotbarButtons;

        public bool ShowFps;

        public UILayoutConfig GetDefaultConfigData()
        {
            return new UILayoutConfig()
            {
                FontSize = 14,
                ThemeColor = "#4A90D9",
                MainMenuPos = new UIPosition()
                {
                    X = 100f,
                    Y = 200f,
                },
                HotbarButtons = new List<UIButtonConfig>()
                {
                    new UIButtonConfig() { Label = "背包", IconIndex = 0, Enabled = true },
                    new UIButtonConfig() { Label = "技能", IconIndex = 1, Enabled = true },
                    new UIButtonConfig() { Label = "设置", IconIndex = 2, Enabled = true },
                },
                ShowFps = false,
            };
        }
    }
}
