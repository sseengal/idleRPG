using UnityEngine;
using UnityEngine.Rendering;

namespace IdleRPG.Art
{
    /// <summary>
    /// Assigns the HD-2D lit sprite material to every SpriteRenderer under this object and turns shadow casting /
    /// receiving on for them.
    ///
    /// Why this exists: URP's stock sprite material is unlit and has no shadow caster pass, so a 2D rig dropped into a
    /// 3D scene renders flat and shadowless no matter what the renderer's own flags say.
    ///
    /// Multi-part rigs (the enemy prefabs are Body / Wing / Mount parts) keep their own sorting order and carved
    /// pivots - only the material and the shadow flags are touched, so the rig and its animations stay intact.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteLitMaterialSwapper : MonoBehaviour
    {
        [Tooltip("Material asset using the HD2D/SpriteLit shader: Assets/IdleRPG/Art/Materials/HD2D_SpriteLit.mat")]
        [SerializeField] private Material spriteLitMaterial;

        [SerializeField] private bool castShadows = true;
        [SerializeField] private bool receiveShadows = true;
        [SerializeField] private bool applyOnAwake = true;

        /// <summary>Renderers swapped by the last <see cref="Apply"/> call.</summary>
        public int SwappedCount { get; private set; }

        private void Awake()
        {
            if (applyOnAwake)
            {
                Apply();
            }
        }

        /// <summary>Swaps the material on every child SpriteRenderer. Safe to call repeatedly.</summary>
        [ContextMenu("Apply HD2D sprite material")]
        public void Apply()
        {
            if (spriteLitMaterial == null)
            {
                Debug.LogWarning("[SpriteLitMaterialSwapper] no material assigned; nothing to do.", this);
                return;
            }

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            int swapped = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = renderers[i];
                spriteRenderer.sharedMaterial = spriteLitMaterial;
                spriteRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                spriteRenderer.receiveShadows = receiveShadows;
                swapped++;
            }

            SwappedCount = swapped;

            if (swapped == 0)
            {
                Debug.LogWarning($"[SpriteLitMaterialSwapper] no SpriteRenderer found under '{name}'.", this);
            }
        }
    }
}
