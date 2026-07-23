using System.Collections.Generic;

namespace WPZ0325.EasyConfig.Demo
{
    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard,
        Expert,
    }

    [System.Serializable]
    public class LevelProgressionConfig : IEasyConfigBase<LevelProgressionConfig>
    {
        public int CurrentLevel;

        public float BestScore;

        public DifficultyLevel Difficulty;

        public List<int> CompletedLevels;

        public bool TutorialDone;

        public LevelProgressionConfig GetDefaultConfigData()
        {
            return new LevelProgressionConfig()
            {
                CurrentLevel = 1,
                BestScore = 0f,
                Difficulty = DifficultyLevel.Normal,
                CompletedLevels = new List<int>(),
                TutorialDone = false,
            };
        }
    }
}
