using System;
using CubeChallenge3D.Economy;
using UnityEngine;

namespace CubeChallenge3D.UI.Common
{
    [DisallowMultipleComponent]
    public sealed class DailyRewardAttentionBinding : MonoBehaviour
    {
        private RewardAttentionEffect attentionEffect;
        private float nextCheckTime;

        private void Awake()
        {
            attentionEffect = GetComponent<RewardAttentionEffect>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheckTime)
            {
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            nextCheckTime = Time.unscaledTime + 1f;
            bool canClaim = new DailyRewardStore().CanClaim(DateTime.UtcNow);
            if (canClaim)
            {
                if (attentionEffect == null)
                {
                    attentionEffect = gameObject.AddComponent<RewardAttentionEffect>();
                }

                attentionEffect.enabled = true;
                return;
            }

            if (attentionEffect != null)
            {
                attentionEffect.enabled = false;
            }
        }
    }
}
