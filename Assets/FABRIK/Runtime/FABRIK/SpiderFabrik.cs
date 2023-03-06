using System.Collections.Generic;
using UnityEngine;

namespace Yohash.FABRIK
{
  public class SpiderFabrik : MonoBehaviour
  {
    public Transform centralHub;
    public int maxIters = 10;

    // all the information for each chain the SpiderFABRIK
    [Header("Chains")]
    public List<FabrikChain> chains;
    public List<Vector3> chainForwards;
    public List<bool> useChain_toCenterHub;

    // if we want to override 'centering' the central hub
    [Header("Central Hub Position Overrides")]
    public bool override_HubAtLocalZero;
    public bool override_HubCentering;
    public int manualCenter_chainIndex;

    // if we want to override any axis of the torso's rotation
    [Header("Central Hub Rotation Overrides")]
    public bool XAxisRotationIgnore;
    public bool YAxisRotationIgnore;
    public bool ZAxisRotationIgnore;

    // for the 'head', if there is to be one
    [Header("Optional Head")]
    public Transform headObjectTransform;
    Quaternion lastHeadRotation;

    [Header("DEBUGGING VARS")]
    public bool DEBUG_SHOWFORWARD;
    public bool DEBUG_SHOW2ndLINK_RelDir;
    public bool DEBUG_SHOWavg2ndLINK;

    public Vector3 DEBUG_FWD = Vector3.zero;
    public Vector3 DEBUG_PLANENORM = Vector3.up;
    public Vector3 DEBUG_projection;


    // ****************************************************************
    //		MONOBEHAVIOURS
    // ****************************************************************
    void Start()
    {
      Transform joint;
      float fwd, rt, up;
      Vector3 v3;
      // find the forward-vector for each joint, relative to the centralHub forwad
      for (int i = 0; i < chains.Count; i++) {
        joint = chains[i].SecondLink;
        rt = Vector3.Dot(centralHub.forward, joint.right);
        fwd = Vector3.Dot(centralHub.forward, joint.forward);
        up = Vector3.Dot(centralHub.forward, joint.up);
        v3 = new Vector3(rt, up, fwd);
        chainForwards.Add(v3);
        chains[i].LocalRelativeForward = v3;
      }
    }

    void Update()
    {
      if (DEBUG_SHOW2ndLINK_RelDir) {
        for (int i = 0; i < useChain_toCenterHub.Count; i++) {
          if (useChain_toCenterHub[i]) {
            // draw its relative forward
            Debug.DrawRay(
              chains[i].SecondLink.position,
              chains[i].SecondLink.TransformDirection(chainForwards[i]),
              Color.red
            );
          }
        }
      }
      if (DEBUG_SHOWavg2ndLINK) {
        Vector3 v = Vector3.zero;
        for (int i = 0; i < useChain_toCenterHub.Count; i++) {
          if (useChain_toCenterHub[i]) {
            // record its relative forward
            v += chains[i].SecondLink.TransformDirection(chainForwards[i]);
          }

          if (headObjectTransform != null) {
            Debug.DrawRay(headObjectTransform.position, headObjectTransform.forward, Color.red);
          }
        }
        v /= chains.Count;
        Debug.DrawRay(centralHub.position, v * 2f, Color.yellow);
      }
      if (DEBUG_SHOWFORWARD) {
        Debug.DrawRay(centralHub.position, centralHub.forward, Color.black);
      }

      DEBUG_projection = Vector3.ProjectOnPlane(DEBUG_FWD, DEBUG_PLANENORM);
    }

    public void SolveIK()
    {
      solve();

      if (headObjectTransform != null) {
        lastHeadRotation = headObjectTransform.rotation;
      }
    }

    // ****************************************************************
    //		SpiderFABRIK solver
    // ****************************************************************
    void solve()
    {
      // setup our iteration counter
      int iter = 0;

      // Check the two conditions that warrant IK updates
      //		1) the targets are not within the range tolerance
      //		2) a 'head' object is declared and it has rotated
      while (!allTargets_areWithinRange()) {
        // declare a variable to track initial position
        var newSub = Vector3.zero;
        // perform a backwards pass over all chains
        for (int i = 0; i < chains.Count; i++) {
          chains[i].Backward();
          // update the base node
          newSub += chains[i].Positions[0];
        }
        // get the average of newSub
        newSub /= chains.Count;

        if (override_HubCentering) {
          // we are to override centering the hub, instead using the
          // provided index.
          newSub = chains[manualCenter_chainIndex].Positions[0];
        }

        if (override_HubAtLocalZero) {
          newSub = transform.TransformPoint(Vector3.zero);
        }

        centralHub.position = newSub;

        // adjust the rotation of the central hub at its new position
        adjustHubRotation();
        // using this position as the root, perform a forward pass
        // perform a forwards pass over all chains
        for (int i = 0; i < chains.Count; i++) {
          chains[i].Forward(newSub);
          chains[i].Move();
        }

        // check current iterations
        if (iter > maxIters)
          break;
        iter += 1;
      }
    }

    private void adjustHubRotation()
    {
      var vn3 = Vector3.zero;
      // adjust the rotation to face in the averaged relative forward vectors
      for (int i = 0; i < useChain_toCenterHub.Count; i++) {
        if (useChain_toCenterHub[i]) {
          // get vector sum of all fabChain, 2nd-chain relativelocalfowards
          vn3 += chains[i].SecondLink.TransformDirection(chains[i].LocalRelativeForward);
        }
      }
      if (headObjectTransform != null) {
        vn3 += headObjectTransform.forward / 2f;
      }

      // get quaternion to rotate our forward to said new forward
      if (vn3 != Vector3.zero) {
        var toQuat = Quaternion.LookRotation(vn3);

        var roteEuler = toQuat.eulerAngles;

        if (XAxisRotationIgnore) { roteEuler.x = 0f; }
        if (YAxisRotationIgnore) { roteEuler.y = 0f; }
        if (ZAxisRotationIgnore) { roteEuler.z = 0f; }

        toQuat = Quaternion.Euler(roteEuler);

        centralHub.rotation = toQuat;
      }
    }

    private bool allTargets_areWithinRange()
    {
      // two conditions warrant a target being 'not within range
      //		1) the targets are not within the range tolerance
      //		2) a 'head' object is declared and it has rotated
      bool withingRange = true;
      for (int i = 0; i < chains.Count; i++) {
        withingRange = (withingRange && chains[i].IsWithinTolerance);
      }

      // now check the head object if it is declared
      if (headObjectTransform != null) {
        if (lastHeadRotation != headObjectTransform.rotation) {
          withingRange = false;
        }
      }
      return withingRange;
    }
  }
}
