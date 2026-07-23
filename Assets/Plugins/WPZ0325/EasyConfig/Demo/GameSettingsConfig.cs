namespace WPZ0325.EasyConfig.Demo
{
    public enum QualityLevel
    {
        Low,
        Medium,
        High,
        Ultra
    }

    [System.Serializable]
    public class GameSettingsConfig : IEasyConfigBase<GameSettingsConfig>
    {
        public float MasterVolume;

        public bool MusicEnabled;

        public QualityLevel Quality;

        public int ResolutionWidth;

        public string Language;

        public GameSettingsConfig GetDefaultConfigData()
        {
            return new GameSettingsConfig()
            {
                MasterVolume = 0.8f,
                MusicEnabled = true,
                Quality = QualityLevel.High,
                ResolutionWidth = 1920,
                Language = "zh-CN",
            };
        }
    }
}
