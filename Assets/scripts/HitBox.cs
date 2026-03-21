using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    public int damage = 10;
    
    // --- 新規追加部分：ガード不能攻撃かどうかのフラグ ---
    public bool isUnblockable = false;
    // ----------------------------------------------

    private Collider hitCollider;
    private Renderer hitRenderer;
    private AudioSource ownerAudioSource;
    private HashSet<FighterStats> hitTargets = new HashSet<FighterStats>();

    void Awake()
    {
        hitCollider = GetComponent<Collider>();
        hitRenderer = GetComponent<Renderer>();
        ownerAudioSource = GetComponentInParent<AudioSource>();

        if (hitCollider != null)
            hitCollider.enabled = false;

        if (hitRenderer != null)
            hitRenderer.enabled = false;
    }

    // --- 新規修正部分：ガード不能フラグを追加 ---
    public void ActivateHitBox(float duration, int newDamage = 10, bool unblockable = false)
    {
        this.damage = newDamage;
        this.isUnblockable = unblockable;
        StartCoroutine(HitRoutine(duration));
    }
    // ----------------------------------------------

    IEnumerator HitRoutine(float duration)
    {
        hitTargets.Clear();

        if (hitCollider != null)
            hitCollider.enabled = true;

        if (hitRenderer != null)
            hitRenderer.enabled = true;

        yield return new WaitForSeconds(duration);

        if (hitCollider != null)
            hitCollider.enabled = false;

        if (hitRenderer != null)
            hitRenderer.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        FighterStats target = other.GetComponent<FighterStats>();

        if (target == null)
            target = other.GetComponentInParent<FighterStats>();

        if (target == null)
            return;

        FighterStats owner = GetComponentInParent<FighterStats>();

        if (owner != null && target == owner)
            return;

        if (hitTargets.Contains(target))
            return;

        if (ownerAudioSource != null)
        {
            ownerAudioSource.Play();
        }

        // --- 修正部分：ガード不能フラグを渡す ---
        target.TakeDamage(damage, transform.position, isUnblockable);
        hitTargets.Add(target);
    }
}