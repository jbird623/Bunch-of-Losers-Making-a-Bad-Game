﻿using UnityEngine;
using System.Collections;

public static class PepsiHealth {
	public static float value = 200f;
}

[RequireComponent(typeof(EnemyStats))]
public class PepsiHealthHandler : MonoBehaviour {

	EnemyStats stats;

	void Awake () {
		stats = GetComponent<EnemyStats>();
		stats.health = PepsiHealth.value;
	}

	void OnDestroy () {
		PepsiHealth.value = stats.health;
	}

}