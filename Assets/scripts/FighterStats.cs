using System.Collections;
using UnityEngine;

public class FighterStats : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;
    public float knockbackPower = 0.5f;

    private Renderer bodyRenderer;
    private Color originalColor;

    public bool IsDead
    {
        get { return currentHP <= 0; }
    }

    void Start()
    {
        currentHP = maxHP;

        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            originalColor = bodyRenderer.material.color;
        }
    }

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        if (IsDead) return;

        currentHP -= damage;

        if (currentHP < 0)
            currentHP = 0;

        Debug.Log(gameObject.name + " HP: " + currentHP);

        ApplyKnockback(attackerPosition);
        StartCoroutine(DamageFlash());
    }

    void ApplyKnockback(Vector3 attackerPosition)
    {
        float dir = transform.position.x > attackerPosition.x ? 1f : -1f;
        transform.Translate(Vector3.right * dir * knockbackPower, Space.World);
    }

    IEnumerator DamageFlash()
    {
        if (bodyRenderer == null) yield break;

        bodyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        bodyRenderer.material.color = originalColor;
    }
}