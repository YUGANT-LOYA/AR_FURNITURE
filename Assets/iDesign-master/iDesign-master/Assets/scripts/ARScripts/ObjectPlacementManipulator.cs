﻿namespace GoogleARCore.Examples.ObjectManipulation
{
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections.Generic;

    /// <summary>
    /// Controls the placement of objects via a tap gesture.
    /// </summary>
    public class ObjectPlacementManipulator : Manipulator
    {
        /// <summary>
        /// The first-person camera being used to render the passthrough camera image (i.e. AR
        /// background).
        /// </summary>
        public Camera FirstPersonCamera;

        /// <summary>
        /// A model to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject ObjectToPlace;

        /// <summary>
        /// Manipulator prefab to attach placed objects to.
        /// </summary>
        public GameObject ManipulatorPrefab;

        private List<GameObject> PlacedObjects;

        private ItemManager itemManager;

        /// <summary>
        /// On object creation: 
        /// Assign the itemManager ObjectToPlace to this scripts ObjectToPlace 
        /// </summary>
        private void Start()
        {
            itemManager = GameObject.Find("Item Manager").GetComponent<ItemManager>();
            ObjectToPlace = itemManager.ObjectToPlace;
            Debug.Log("OPM: " + ObjectToPlace + " itemManager: " + itemManager.ObjectToPlace);
            PlacedObjects = new List<GameObject>();
        }
       
        protected override bool CanStartManipulationForGesture(TapGesture gesture)
        {
            if (checkPlaceToggle() == false)
            {
                return false;
            }

            if (gesture.TargetObject == null)
            {
                return true;
            }

            return false;
        }

        protected override void OnEndManipulation(TapGesture gesture)
        {

            if (gesture.WasCancelled)
            {
                return;
            }

            // If gesture is targeting an existing object we are done.
            if (gesture.TargetObject != null)
            {
                return;
            }

            // Raycast against the location the player touched to search for planes.
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;

            if (Frame.Raycast(
                gesture.StartPosition.x, gesture.StartPosition.y, raycastFilter, out hit))
            {
                // Use hit pose and camera pose to check if hittest is from the
                // back of the plane, if it is, no need to create the anchor.
                if ((hit.Trackable is DetectedPlane) &&
                    Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                        hit.Pose.rotation * Vector3.up) < 0)
                {
                    Debug.Log("Hit at back of the current DetectedPlane");
                }
                else
                {
                    ObjectToPlace = itemManager.ObjectToPlace;

                    // Instantiate model at the hit pose.
                    var modelObject = Instantiate(ObjectToPlace, hit.Pose.position, hit.Pose.rotation);

                    // Instantiate manipulator.
                    var manipulator =
                        Instantiate(ManipulatorPrefab, hit.Pose.position, hit.Pose.rotation);

                    // Make model a child of the manipulator.
                    modelObject.transform.parent = manipulator.transform;

                    // Create an anchor to allow ARCore to track the hitpoint as understanding of
                    // the physical world evolves.
                    var anchor = hit.Trackable.CreateAnchor(hit.Pose);

                    // Make manipulator a child of the anchor.
                    manipulator.transform.parent = anchor.transform;

                    // Select the placed object.
                    manipulator.GetComponent<Manipulator>().Select();

                    //Disable object placement after item placed.
                    GameObject manipulationPanel = GameObject.Find("Controls");
                    manipulationPanel.GetComponent<ManipulationButtons>().togglePlace = false;

                    PlacedObjects.Add(modelObject);
                    GameObject.Find("Controls").GetComponent<ManipulationButtons>().TogglePressedColour();

                }
            }
        }

        //Checks if place is enabled.
        private bool checkPlaceToggle()
        {
            GameObject manipulationPanel = GameObject.Find("Controls");

            return manipulationPanel.GetComponent<ManipulationButtons>().GetPlaceStatus();
        }

        //Delete item that currently has visualization enabled
        //If the item has visualization enabled, then that will be the item
        //currently selected.
        public void DeleteItem()
        {
            
            foreach (GameObject placedObject in PlacedObjects)
            {
                GameObject DeleteObject = placedObject.transform.parent.gameObject;
                if (DeleteObject.transform.Find("Selection Visualization").gameObject.activeSelf)
                {
                    PlacedObjects.Remove(DeleteObject);
                    DeleteObject.SetActive(false);
                    //Destroy(DeleteObject);
                }
            }
        }
        
        public void ChangeObjectToPlace(GameObject toObject)
        {
            ObjectToPlace = toObject;
        }
    }
}
