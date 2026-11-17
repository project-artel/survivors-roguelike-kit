#region

using System.Collections;
using RGame.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

#endregion

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Manages scene loading and unloading operations in the game.
    /// Handles transitions between gameplay and menu scenes with loading screens and fade effects.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [Header("Scene References")] 
        [SerializeField] private GameSceneSO gameplaySceneSO;

        [Header("Input System")] 
        [SerializeField] private InputReader inputReader;

        [Header("Event Channels - Listen")] 
        [SerializeField] private LoadEventChannelSO loadLevelChannel;
        [SerializeField] private LoadEventChannelSO loadMenuChannel;

        [Header("Event Channels - Broadcast")] 
        [SerializeField] private BoolEventChannelSO toggleLoadingScreenChannel;
        [SerializeField] private FadeChannelSO fadeRequestChannel;
        [SerializeField] private VoidEventChannelSO sceneReadyChannel;

        private const float FadeDuration = 1.5f;
        private bool isLoading;
        private GameSceneSO currentLoadedScene;
        private GameSceneSO pendingSceneToLoad;
#if ARTEL_GAME_FUNCTIONS
        private GameSceneSO qaMenuScene;
#endif
        
        private AsyncOperationHandle<SceneInstance> gameplayManagerLoadingHandle;
        private SceneInstance gameplayManagerSceneInstance;
        private AsyncOperationHandle<SceneInstance> currentSceneLoadingHandle;

        private void OnEnable()
        {
            loadLevelChannel.OnLoadingRequested += HandleLevelLoadRequest;
            loadMenuChannel.OnLoadingRequested += HandleMenuLoadRequest;
        }

        private void OnDisable()
        {
            loadLevelChannel.OnLoadingRequested -= HandleLevelLoadRequest;
            loadMenuChannel.OnLoadingRequested -= HandleMenuLoadRequest;
        }

        private void HandleLevelLoadRequest(GameSceneSO sceneToLoad, bool showLoadingScreen, bool fadeScreen)
        {
            if (isLoading) return;

            pendingSceneToLoad = sceneToLoad;
            isLoading = true;

            if (!IsGameplayManagerLoaded())
            {
                LoadGameplayManager();
            }
            else
            {
                StartCoroutine(UnloadCurrentSceneRoutine());
            }
        }

        private void HandleMenuLoadRequest(GameSceneSO menuToLoad, bool showLoadingScreen, bool fadeScreen)
        {
            if (isLoading) return;

#if ARTEL_GAME_FUNCTIONS
            qaMenuScene = menuToLoad;
#endif
            pendingSceneToLoad = menuToLoad;
            isLoading = true;

            if (IsGameplayManagerLoaded())
            {
                UnloadGameplayManager();
            }

            StartCoroutine(UnloadCurrentSceneRoutine());
        }

#if ARTEL_GAME_FUNCTIONS
        internal IEnumerator ReloadMenuForQa()
        {
            Time.timeScale = 1f;
            while (isLoading)
                yield return null;

            if (!qaMenuScene)
                throw new System.InvalidOperationException("메인 메뉴가 아직 초기화되지 않았습니다.");

            var buffManager = FindAnyObjectByType<BuffManager>();
            if (buffManager)
                buffManager.ClearAllBuffs();
            SurvivorsQaState.Loaded<PoolRuntimeSO>()?.RecycleAll();

            var save = SurvivorsQaState.Loaded<SaveSystemSO>();
            if (save && FileManager.LoadFromFile(save.saveFilename, out var json) && !string.IsNullOrEmpty(json))
                save.LoadSaveDataFromDisk();

            HandleMenuLoadRequest(qaMenuScene, true, true);
            while (isLoading)
                yield return null;
        }
#endif

        private bool IsGameplayManagerLoaded()
        {
            return gameplayManagerSceneInstance.Scene != null && gameplayManagerSceneInstance.Scene.isLoaded;
        }

        private void LoadGameplayManager()
        {
            gameplayManagerLoadingHandle = gameplaySceneSO.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
            gameplayManagerLoadingHandle.Completed += OnGameplayManagerLoaded;
        }

        private void UnloadGameplayManager()
        {
            Addressables.UnloadSceneAsync(gameplayManagerLoadingHandle);
        }

        private void OnGameplayManagerLoaded(AsyncOperationHandle<SceneInstance> handle)
        {
            gameplayManagerSceneInstance = gameplayManagerLoadingHandle.Result;
            StartCoroutine(UnloadCurrentSceneRoutine());
        }

        private IEnumerator UnloadCurrentSceneRoutine()
        {
            inputReader.DisableAllInput();
            fadeRequestChannel.FadeOut(FadeDuration);

            yield return new WaitForSeconds(FadeDuration);

            toggleLoadingScreenChannel.RaiseEvent(true);

            if (currentLoadedScene != null && currentLoadedScene.sceneReference.OperationHandle.IsValid())
            {
                var unload = currentLoadedScene.sceneReference.UnLoadScene();
                if (currentLoadedScene == pendingSceneToLoad)
                    yield return unload;
            }

            LoadPendingScene();
        }

        private void LoadPendingScene()
        {
            currentSceneLoadingHandle = pendingSceneToLoad.sceneReference.LoadSceneAsync(
                LoadSceneMode.Additive, 
                true, 
                0);
                
            currentSceneLoadingHandle.Completed += OnSceneLoaded;
        }

        private void OnSceneLoaded(AsyncOperationHandle<SceneInstance> handle)
        {
            currentLoadedScene = pendingSceneToLoad;
            SceneManager.SetActiveScene(handle.Result.Scene);
            LightProbes.TetrahedralizeAsync();

            isLoading = false;
            toggleLoadingScreenChannel.RaiseEvent(false);
            fadeRequestChannel.FadeIn(FadeDuration);
            sceneReadyChannel.RaiseEvent();
        }
    }
}
