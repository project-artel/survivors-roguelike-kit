#if ARTEL_GAME_FUNCTIONS
using System;
using System.Collections;
using Artel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RGame.RoguelikeKit
{
    public static class SurvivorsGameFunctions
    {
        public enum QaCharacter { Assassin, Mage }

        [ArtelCheat("메뉴에서 캐릭터 해금 상태를 저장합니다. 적용 후 게임을 재시작하면 카드와 선택 상태가 다시 초기화됩니다.", when: nameof(InMenu))]
        private static string SetCharacterUnlocked(QaCharacter character, bool unlocked)
        {
            var save = SurvivorsQaState.Loaded<CharacterSaveSO>();
            if (!save) throw new InvalidOperationException("캐릭터 저장 데이터가 없습니다.");
            save.SaveCharacter(character == QaCharacter.Assassin ? unlocked : save.CharacterTwo,
                character == QaCharacter.Mage ? unlocked : save.CharacterThree);
            SaveQaPreparation();
            return $"character={character}, unlocked={unlocked}; 게임을 재시작하세요.";
        }

        [ArtelCheat("메뉴에서 Might 영구 강화 단계를 0으로 저장합니다. 게임 재시작 후 강화 수치와 카드가 다시 초기화됩니다. 골드는 유지합니다.", when: nameof(InMenu))]
        private static string ResetMightUpgrade()
        {
            var attributes = SurvivorsQaState.Loaded<AttributeSO>();
            attributes.SetAttribute("Might", 0);
            SaveQaPreparation();
            return "might_upgrade_level=0; 게임을 재시작하세요.";
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ArtelCheat("현재 플레이어의 무적을 켜거나 끕니다. 켜면 적 공격의 피해·피격·사망 처리를 막습니다. 새 플레이어에서는 꺼집니다.", when: nameof(Playing))]
        private static string SetInvincible(bool enabled)
        {
            var hit = Object.FindAnyObjectByType<PlayerHit>();
            if (!hit) throw new InvalidOperationException("플레이어 피격 컴포넌트가 없습니다.");
            hit.QaInvincible = enabled;
            return $"invincible={hit.QaInvincible}";
        }
#endif

        [ArtelCheat("전투의 현재 체력을 1 이상으로 준비합니다. 사망은 실제 적 공격으로 검증합니다.", when: nameof(Playing))]
        private static string SetHealth([CheatRange(1, 100000)] int health)
            => Object.FindAnyObjectByType<LevelManager>().SetQaHealth(health);

        [ArtelCheat("메뉴의 보유 골드를 설정하고 기존 저장 API로 저장합니다. 구매 동작은 수행하지 않습니다.", when: nameof(InMenu))]
        private static string SetGold([CheatRange(0, 1000000)] int gold)
        {
            var attributes = SurvivorsQaState.Loaded<AttributeSO>();
            attributes.SetAttribute("GoldCount", gold);
            SurvivorsQaState.Loaded<SaveSystemSO>().SaveDataToDisk();
            return $"gold={attributes.GoldCount}; 다시 연 메뉴에서 표시를 확인하세요.";
        }

        private static void SaveQaPreparation()
        {
            var save = SurvivorsQaState.Loaded<SaveSystemSO>();
            if (!System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath, save.saveFilename)))
                save.WriteEmptySaveFile();
            save.SaveDataToDisk();
            if (!FileManager.LoadFromFile(save.saveFilename, out var json) || json != save.Save.ToJson())
                throw new InvalidOperationException("QA 준비 상태를 저장하지 못했습니다.");
        }

        private static string InMenu() => Object.FindAnyObjectByType<UIMenuManager>() &&
            SurvivorsQaState.Loaded<AttributeSO>() && SurvivorsQaState.Loaded<SaveSystemSO>()
                ? null : "메인 메뉴가 필요합니다.";

        [ArtelCheat("다음 레벨까지 남은 경험치를 설정합니다. 레벨업은 경험치를 획득할 때 일어납니다.",
            when: nameof(Playing))]
        private static string SetXpToNextLevel([CheatRange(1, int.MaxValue)] int remainingXp)
        {
            var levels = Object.FindAnyObjectByType<LevelManager>();
            if (!levels) throw new InvalidOperationException("레벨 관리자가 활성화되어 있지 않습니다.");
            return levels.SetQaXpToNextLevel(remainingXp);
        }

        [ArtelCheat("현재 스테이지의 포위 또는 밀집 스폰 세트를 이름으로 실행합니다. 이름을 비우면 선택 가능한 세트를 표시합니다.",
            when: nameof(Playing))]
        private static string SpawnEnemyGroup(string spawnSet = "")
        {
            return Object.FindAnyObjectByType<StageManager>().SpawnQaEnemyGroup(spawnSet);
        }

        [ArtelBehavior("키보드를 거치지 않고 기존 이동 경로로 가장 가까운 적에게서 멀어집니다. 입력 검증에는 사용하지 않습니다. 일시정지에서 종료합니다.",
            group: "movement", when: nameof(Playing))]
        private static IEnumerator KiteEnemies([CheatRange(1, 120)] int seconds, ArtelBehaviorContext context)
        {
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            var stage = Object.FindAnyObjectByType<StageManager>();
            float elapsed = 0f;
            float nextDirectionAt = 0f;
            try
            {
                while (player && player.IsQaPlayerAlive && stage && stage.IsQaStageReady &&
                       Time.timeScale > 0f && elapsed < seconds)
                {
                    if (elapsed >= nextDirectionAt)
                    {
                        player.QaMoveDirection = stage.GetQaKiteDirection(player.transform.position);
                        nextDirectionAt = elapsed + 0.1f;
                    }
                    context.Report($"elapsed={elapsed:F1}/{seconds}");
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                context.Complete(Time.timeScale == 0f ? "전투가 일시정지되었습니다." : "적 회피를 종료했습니다.");
            }
            finally
            {
                if (player) player.QaMoveDirection = Vector2.zero;
            }
        }

        private static string Playing()
        {
            if (Time.timeScale == 0f) return "전투가 일시정지되었습니다.";
            var stage = Object.FindAnyObjectByType<StageManager>();
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            return stage && stage.IsQaStageReady && player && player.IsQaPlayerAlive
                ? null
                : "플레이 중인 전투가 필요합니다.";
        }
    }
}
#endif
