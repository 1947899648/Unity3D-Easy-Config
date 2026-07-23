using System.Collections.Generic;

namespace WPZ0325.EasyConfig.Demo
{
    [System.Serializable]
    public class CharacterStats
    {
        public int Strength;
        public int Agility;
        public int Intelligence;
        public int Stamina;
    }

    [System.Serializable]
    public class PlayerProfileConfig : IEasyConfigBase<PlayerProfileConfig>
    {
        public string PlayerName;

        public int Level;

        public long Gold;

        public byte VipLevel;

        public List<string> UnlockedHeroes;

        public CharacterStats Stats;

        public PlayerProfileConfig GetDefaultConfigData()
        {
            return new PlayerProfileConfig()
            {
                PlayerName = "Hero",
                Level = 1,
                Gold = 10000L,
                VipLevel = 0,
                UnlockedHeroes = new List<string>()
                {
                    "Warrior",
                    "Mage",
                },
                Stats = new CharacterStats()
                {
                    Strength = 10,
                    Agility = 8,
                    Intelligence = 5,
                    Stamina = 12,
                },
            };
        }
    }
}
