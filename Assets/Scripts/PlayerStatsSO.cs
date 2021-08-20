using UnityEngine;

[CreateAssetMenu(fileName = "Player Stats", menuName = "Scriptable Objects/Player Stat", order = 1)]
public class PlayerStatsSO : ScriptableObject
{
	public float walkSpeed = 2;
	public float jumpForce = 2;
	public float maxHealth = 3;
	public float currentHealth = 3;
}