using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine.Pool;

namespace UnityEngine.AddressableAssets
{
    public static partial class AddressablesManager
    {
        public static Dictionary<Scene, HashSet<string>> SceneToAddress = new();

        private static bool _isInitialized;

        public static void DockTo<T>(this UniTask<OperationResult<T>> operation, string assetKey,
            Scene sceneDocker) where T : Object
        {
            if (!GuardKey(assetKey, out var key))
                return;
            SubscribeSceneEvents();
            if (!SceneToAddress.TryGetValue(sceneDocker, out var addressList))
            {
                addressList = CollectionPool<HashSet<string>, string>.Get();
                addressList.Add(key);
                SceneToAddress.Add(sceneDocker, addressList);
            }

            if (!addressList.Contains(key))
                addressList.Add(key);
        }

        public static UniTask<OperationResult<T>> DockTo<T>(this UniTask<OperationResult<T>> operation,
            AssetReferenceT<T> reference, Scene sceneDocker) where T : Object
        {
            if (GuardKey(reference, out var key))
                DockTo(operation, key, sceneDocker);
            return operation;
        }

        public static void OnSceneUnloaded(Scene sceneUnload)
        {
            if (SceneToAddress.TryGetValue(sceneUnload, out var addressList))
            {
                foreach (var address in addressList)
                    ReleaseAsset(address);
                SceneToAddress.Remove(sceneUnload);
                CollectionPool<HashSet<string>, string>.Release(addressList);
                Resources.UnloadUnusedAssets();
            }
        }

        private static void SubscribeSceneEvents()
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            Application.quitting += OnApplicationQuit;
        }
        
        private static void OnApplicationQuit()
        {
            _isInitialized = false;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Application.quitting -= OnApplicationQuit;
        }
    }
}