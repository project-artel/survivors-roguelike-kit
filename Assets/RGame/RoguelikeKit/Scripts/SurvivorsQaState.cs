#if ARTEL_GAME_FUNCTIONS
using System.Linq;
using Artel;
using Artel.Affordances;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    // 기존 게임 값만 읽습니다. 판정을 위해 게임 이벤트나 저장을 발생시키지 않습니다.
    public sealed class SurvivorsQaState : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<SurvivorsQaState>()) return;
            var observer = new GameObject("Survivors QA State");
            DontDestroyOnLoad(observer);
            observer.AddComponent<SurvivorsQaState>();
        }

        internal static T Loaded<T>() where T : UnityEngine.Object
            => Resources.FindObjectsOfTypeAll<T>().SingleOrDefault();

        private static bool Active<T>() where T : Behaviour
            => FindAnyObjectByType<T>() is T component && component.isActiveAndEnabled;

        [ArtelObserve] public float TimeScale => Time.timeScale;
        [ArtelObserve] public bool MenuReady => Active<UIMenuManager>();
        [ArtelObserve] public bool CharacterSelectOpen => Active<CharacterSelectPanel>();
        [ArtelObserve] public bool LevelSelectOpen => Active<LevelSelectPanel>();
        [ArtelObserve] public bool SettingsOpen => Active<SettingPanel>();
        [ArtelObserve] public bool PowerUpOpen => Active<PowerUpPanel>();
        [ArtelObserve] public bool BattleReady => FindAnyObjectByType<StageManager>() is StageManager stage && stage.IsQaStageReady;
        [ArtelObserve] public int Hp => BattleReady ? FindAnyObjectByType<LevelManager>().QaHp : -1;
        [ArtelObserve] public int Level => BattleReady ? FindAnyObjectByType<LevelManager>().QaLevel : -1;
        [ArtelObserve] public int Exp => BattleReady ? FindAnyObjectByType<LevelManager>().QaExp : -1;
        [ArtelObserve] public Vector3 PlayerPosition => FindAnyObjectByType<PlayerMovement>() is PlayerMovement player ? player.transform.position : Vector3.zero;
        [ArtelObserve] public int Gold => Loaded<AttributeSO>() is AttributeSO wallet ? wallet.GoldCount : -1;
        [ArtelObserve] public int MightUpgradeLevel => Loaded<AttributeSO>() is AttributeSO attributes ? attributes.Might : -1;
        [ArtelObserve] public float MasterVolume => Loaded<SettingsSO>() is SettingsSO settings ? settings.MasterVolume : -1f;
        [ArtelObserve] public string Language => Loaded<SettingsSO>() is SettingsSO settings && settings.LanguageLocale ? settings.LanguageLocale.Identifier.Code : "";

        [ArtelObserve, ArtelMetadata(Kind = "id", Identifies = "level", Role = "current",
            Meaning = "메뉴에서 현재 선택한 레벨 데이터의 이름입니다. 같은 맵을 쓰는 레벨도 구분합니다.",
            ChangesWhen = "레벨 카드를 클릭할 때 바뀌며 전투 진입 후에도 선택 데이터에 유지됩니다.")]
        public string SelectedLevel => Loaded<LevelAndCharacterSO>() is LevelAndCharacterSO selection && selection.SelectLevelSO ? selection.SelectLevelSO.name : "";

        [ArtelObserve, ArtelMetadata(Kind = "id", Identifies = "character", Role = "current",
            Meaning = "현재 선택 데이터의 캐릭터입니다. 해금 여부와는 별개입니다.",
            ChangesWhen = "캐릭터 카드의 클릭 이벤트가 실행될 때 바뀝니다.")]
        public string SelectedCharacter => Loaded<LevelAndCharacterSO>() is LevelAndCharacterSO selection && selection.SelectCharacterSo ? selection.SelectCharacterSo.CharacterName : "";
    }
}
#endif
