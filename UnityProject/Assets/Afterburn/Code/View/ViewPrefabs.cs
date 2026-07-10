using UnityEngine;

namespace Afterburn.View
{
    /// <summary>
    /// Vendor-prefab instantiation helper. Afterburn's collision is the analytic lateral clamp —
    /// vendor colliders are dead weight, and Ebal's mirrored (negative-scale) modules make
    /// BoxColliders spam warnings the moment physics registers them. So: instantiate under an
    /// inactive holder, strip every collider BEFORE activation, then release the instance.
    /// </summary>
    internal static class ViewPrefabs
    {
        public static GameObject InstantiateWithoutColliders(GameObject prefab, Transform parent)
        {
            var holder = new GameObject("~PrefabStaging");
            holder.transform.SetParent(parent, false);
            holder.SetActive(false);                            // children spawn unregistered

            GameObject instance = Object.Instantiate(prefab, holder.transform);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);              // gone before physics ever sees them
            }

            instance.transform.SetParent(parent, false);        // re-activates under the real parent
            Object.Destroy(holder);
            return instance;
        }
    }
}
