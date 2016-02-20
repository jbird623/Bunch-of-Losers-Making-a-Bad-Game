﻿using UnityEngine;
using System.Collections;
using JBirdEngine;

public class TestScript : MonoBehaviour {

	public JBirdEngine.ColorLibrary.ColorHelper.ColorHSVRGB hsvrgb;
	public JBirdEngine.ColorLibrary.ColorHelper.ColorHSV hsv;
	public Markov.NameGenerator nameGenerator;

    enum TestEnum {
        ohgeezrick,
        thisisatest,
    }

	void Start () {
		JBirdEngine.AI.AIHelper.GetHeuristic(Vector3.zero, Vector3.one, JBirdEngine.AI.AIHelper.HeuristicMode.hexagonal);
        "thisisatest".ToEnum<TestEnum>();
	}

}