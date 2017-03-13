﻿// SK

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour {
    // Inspector manipulated attributes
    //public GameObject                   divider;
    public Text                         ui_phase;
    public Text                         ui_timeLeft;
    public LayerMask                    layerMask;
    public GameObject                   ground;
    public GameObject[]                 backgroundObjects;

    public GameObject                   LaunchCautionUI;
	public AudioClip					buildBgm;
	public AudioClip 					battleBgm;
    public AudioClip                    countdownAudio;
    public GameObject[]                 rockets;
    public GameObject                   explosion;

    // JF: Disable and reenable these depending on phase
    public GameObject[]                 itemSpawners;

    // Encapsulated attributes
    public static PhaseManager          S;
    public bool                         inBuildPhase = true;
    public bool                         countdownNotStarted = true;
    public int                          currentRound = 0;
    public bool                         gameOver = false;
    public List<GameObject>             placedBlocks;
    private Vector3                     groundDestination;
    public float                        flyingSpeed = 2f;
    private Vector3                     groundStartPosition;
    public float                        gravityScale = 0.9f;

    // Timer
    private float                       timeLeft;
    private string                      seconds;

    // Gameplay Variables
    public float                       build_time = 5;
    public float                       battle_time = 5;
    public int                         rounds_to_play = 2;

	// Components
	private AudioSource					audioSource;

    private void Awake() {
        S = this;
    }
    // Use this for initialization
    void Start () {
        placedBlocks = new List<GameObject>();
        timeLeft = build_time;
        groundDestination = new Vector2(0, -25f);
        groundStartPosition = new Vector2(0, -7f);

		audioSource = GetComponent<AudioSource> ();
    }
	
	// Update is called once per frame
	void Update () {
        if (gameOver) {
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft % 60 < 11 && countdownNotStarted) {
            SwitchToCountdownPhase();
            // else {
            //     SwitchToBuildPhase();
            // }
        }
        else {
            if (inBuildPhase) {
                seconds = (timeLeft % 60).ToString("00");
                ui_timeLeft.text = seconds;
            }
        }
	}

    public void SwitchToCountdownPhase() {
        countdownNotStarted = false;
        audioSource.PlayOneShot(countdownAudio, 3.0f);
        Invoke ("SwitchToBattlePhase", 10);
        InvokeRepeating ("FlashCautionUI", 0, 1);
        ui_timeLeft.fontSize = 50;

        foreach (GameObject obj in itemSpawners) {
            obj.GetComponent<Spawner> ().on = false;
        }
    }

    // Switches to battle phase:
    // Resets timer, makes divider more transparent and allows projectiles through
    public void SwitchToBattlePhase() {
        inBuildPhase = false;
        
        // JF: Stop caution UI from flashing
        LaunchCautionUI.SetActive(false);
        CancelInvoke ("FlashCautionUI");

        StartCoroutine(moveGround(Vector3.down, groundDestination));
        timeLeft = battle_time;
        ui_phase.text = "BATTLE";
        ui_timeLeft.text = "";
		audioSource.clip = battleBgm;
		audioSource.Play ();

        foreach (GameObject obj in backgroundObjects) {
            obj.GetComponent<Rigidbody2D>().gravityScale = gravityScale;
            if(obj.GetComponent<SpaceBackground>() != null) {
                obj.GetComponent<SpaceBackground>().StartFlying();
            }
        }

        foreach (GameObject obj in rockets) {
            obj.GetComponent<LoopingAnimation>().StartAnimation();
        }

        foreach (GameObject go in placedBlocks) {
            go.GetComponent<Rigidbody2D>().gravityScale = 1;
        }
    }

    public void SwitchToBuildPhase() {
        inBuildPhase = true;
        timeLeft = build_time;
        StartCoroutine(moveGround(Vector3.up, groundStartPosition));
        ui_phase.text = "BUILD";
		audioSource.clip = buildBgm;
		audioSource.Play ();

        foreach (GameObject obj in backgroundObjects) {
            obj.GetComponent<Rigidbody2D>().gravityScale = 0;
        }

        foreach (GameObject obj in itemSpawners) {
            obj.GetComponent<Spawner>().on = true;
        }

    }

    IEnumerator moveGround(Vector3 direction, Vector3 destination) {
        float starttime = Time.time;
        while(ground.transform.position != destination) {
            ground.transform.position = Vector3.MoveTowards(ground.transform.position, destination, (Time.time - starttime) * flyingSpeed);
            yield return null;
            }
        }

    // Sets gameOver to true and creates an explosion at the losing team's core
    public void EndGame(GameObject destroyedCore) {
        int winner = 1;
        if (destroyedCore.transform.position.x < 0) {
            winner = 2;
        }
        GameObject boom = Instantiate(explosion, destroyedCore.transform.position, Quaternion.identity);
        boom.GetComponent<LoopingAnimation>().StartAnimation();
        gameOver = true;
        ui_phase.text = "Team " + winner + " wins!";
    }

    private void FlashCautionUI () {
        LaunchCautionUI.SetActive(!LaunchCautionUI.activeInHierarchy);
    }

    // Functions used in "build-to-height" game mode

    /*    
    // Switches to build phase:
    // Checks if either team is above the goal line (if yes, end game)
    // Checks if the number of rounds have reached the total selected (if yes, end game)
    // If neither of these are true, reset timer, change divider to opaque and stop projectiles
    public void SwitchToBuildPhase() {
        int winner = CheckForWinner(7f);
        if(winner != 0) {
            EndGame(winner);
            return;
        }
        currentRound++;
        if(currentRound >= rounds_to_play) {
            winner = FindWinner();
            EndGame(winner);
            return;
        }
        inBuildPhase = true;
        timeLeft = build_time;
        divider.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f);
        divider.layer = LayerMask.NameToLayer("Default");
        ui_phase.text = "BUILD";

        foreach (GameObject obj in itemSpawners) {
            obj.GetComponent<Spawner> ().on = true;
        }
    }

    // Casts a raycast left and a raycast right at the desired height to see if a tower is at that height
    // Only left    -> 1
    // Only right   -> 2
    // Both         -> -1
    // Neither      -> 0
    public int CheckForWinner(float height) {
        RaycastHit2D hitLeft = Physics2D.Raycast(new Vector3(0, height), -Vector2.right, 14, layerMask);
        RaycastHit2D hitRight = Physics2D.Raycast(new Vector3(0, height), Vector2.right, 14, layerMask);

        if (hitLeft.collider != null && hitRight.collider == null) {
            return 1;
        }
        else if (hitRight.collider != null && hitLeft.collider == null) {
            return 2;
        }
        else if(hitLeft.collider != null && hitRight.collider != null) {
            return -1;
        }
        else return 0;
    }

    // calls CheckForWinner at each block height moving down from the goalline until it finds higher tower
    public int FindWinner() {
        int winningTeam = 0;
        float raycastHeight = 8;

        while(winningTeam == 0 && raycastHeight > -6) {
            winningTeam = CheckForWinner(raycastHeight);
            raycastHeight--;
        }
        return winningTeam;
    }

    // Stops the timer, displays the winning team
    public void EndGame(int winner) {
        gameOver = true;
        ui_timeLeft.enabled = false;
        print("Winner was " + winner);
        if(winner == -1 || winner == 0) {
            ui_phase.text = "DRAW!";
        }
        else {
            ui_phase.text = "TEAM " + winner + " WINS!";
        }
    }
    */
}
