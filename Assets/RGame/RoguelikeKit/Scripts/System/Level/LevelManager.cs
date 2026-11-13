 using System;
 using System.Collections;
 using System.Collections.Generic;
using RGame.CommonStat;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private GlobalConfigSO _globalConfig;
        [SerializeField] private CommonStatRuntimeSO _stat;
        [SerializeField] private VoidEventChannelSO _updateLevelUIChannel;
        [SerializeField] private VoidEventChannelSO _levelUpgradeChannel;
        [SerializeField] private PoolRuntimeSO _poolRuntime;
        [SerializeField] private ExpConfig expConfig;

        private void Start()
        {
            _globalConfig.OnGameStart();
            _stat.ResetAllValues();
            _stat.AddAction("Exp",ExpChange);
        }

        private void ExpChange(int value, int maxValue)
        {
            int level = _stat.GetValue("Level");

            if (value >= expConfig.GetExperienceForLevel(level))
            {
                _stat.ModifyValue("Exp", expConfig.GetExperienceForLevel(level) * -1);
                _stat.ModifyValue("Level", 1);
                _levelUpgradeChannel.RaiseEvent();
            }

            _updateLevelUIChannel.RaiseEvent();
        }

#if ARTEL_GAME_FUNCTIONS
        internal int QaHp => _stat.GetValue("HP");
        internal int QaLevel => _stat.GetValue("Level");
        internal int QaExp => _stat.GetValue("Exp");

        internal string SetQaHealth(int health)
        {
            int maximum = _stat.GetMaxValue("HP");
            if (health > maximum) throw new ArgumentOutOfRangeException(nameof(health));
            _stat.ModifyValue("HP", health - _stat.GetValue("HP"));
            return $"hp={_stat.GetValue("HP")}, max_hp={maximum}";
        }

        internal string SetQaXpToNextLevel(int remainingXp)
        {
            int threshold = expConfig.GetExperienceForLevel(_stat.GetValue("Level"));
            if (remainingXp > threshold)
                throw new ArgumentOutOfRangeException(nameof(remainingXp), "현재 레벨의 필요 경험치를 초과했습니다.");

            _stat.ModifyValue("Exp", threshold - remainingXp - _stat.GetValue("Exp"));
            return $"remaining_xp={threshold - _stat.GetValue("Exp")}";
        }
#endif
    }
}
