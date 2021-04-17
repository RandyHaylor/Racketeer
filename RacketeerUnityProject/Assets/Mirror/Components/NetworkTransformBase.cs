// vis2k:
// base class for NetworkTransform and NetworkTransformChild.
// New method is simple and stupid. No more 1500 lines of code.
//
// Server sends current data.
// Client saves it and interpolates last and latest data points.
//   Update handles transform movement / rotation
//   FixedUpdate handles rigidbody movement / rotation
//
// Notes:
// * Built-in Teleport detection in case of lags / teleport / obstacles
// * Quaternion > EulerAngles because gimbal lock and Quaternion.Slerp
// * Syncs XYZ. Works 3D and 2D. Saving 4 bytes isn't worth 1000 lines of code.
// * Initial delay might happen if server sends packet immediately after moving
//   just 1cm, hence we move 1cm and then wait 100ms for next packet
// * Only way for smooth movement is to use a fixed movement speed during
//   interpolation. interpolation over time is never that good.
//
using System;
using UnityEngine;

namespace Mirror
{
    public abstract class NetworkTransformBase : NetworkBehaviour
    {
        [Header("Authority")]
        [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
        public bool clientAuthority;

        /// <summary>
        /// We need to store this locally on the server so clients can't request Authority when ever they like
        /// </summary>
        bool clientAuthorityBeforeTeleport;

        // Is this a client with authority over this transform?
        // This component could be on the player object or any object that has been assigned authority to this client.
        bool IsClientWithAuthority => hasAuthority && clientAuthority;

        [Header("BYPASS INTERPOLATION")]
        [Tooltip("For use in conjunction with NetworkRigidbody.")]
        public bool AlwaysTeleport;

        // Sensitivity is added for VR where human players tend to have micro movements so this can quiet down
        // the network traffic.  Additionally, rigidbody drift should send less traffic, e.g very slow sliding / rolling.
        [Header("Sensitivity")]
        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        public float localPositionSensitivity = .01f;
        [Tooltip("If rotation exceeds this angle, it will be transmitted on the network")]
        public float localRotationSensitivity = .01f;
        [Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
        public float localScaleSensitivity = .01f;


        [Header("Rigidbody Velocity Approximation")]
        [Tooltip("Apply a guess at a speed to the rigidbody so the local physics engine can continue to guess at object movement between updates.")]
        public bool applyExperimentalRigidbodyVelocity = false;

        [Header("Compression")]
        [Tooltip("Enables smallest-three quaternion compression, which is lossy. Great for 3D, not great for 2D where minimal sprite rotations would look wobbly.")]
        public bool compressRotation; // disabled by default to not break 2D projects

        [Header("Interpolation")]
        [Tooltip("Set to true if scale should be interpolated, false is ideal for instant sprite flipping.")]
        public bool interpolateScale = true;


        [Header("Synchronization")]
        // It should be very rare cases that people want to continuously sync scale, true by default to not break previous projects that use it
        // Users in most scenarios are best to send change of scale via cmd/rpc, syncvar/hooks, only once, and when required.  Saves instant 12 bytes (25% of NT bandwidth!)
        [Tooltip("Set to false to not continuously send scale data, and save bandwidth.")]
        public bool syncScale = true;
        [Tooltip("Disable to never sync transform rotation")]
        public bool syncRotation = true;

        float previousSenderTimeStamp = 0;

        // target transform to sync. can be on a child.
        protected abstract Transform targetComponent { get; }

        // server
        Vector3 lastPosition;
        Quaternion lastRotation;
        Vector3 lastScale;

        // client
        public class DataPoint
        {
            public float senderTimeStamp;
            public float timeStamp;
            // use local position/rotation for VR support
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public float movementSpeed;
        }
        // interpolation start and goal
        DataPoint start;
        DataPoint goal;

        // local authority send time
        float lastClientSendTime;

        // serialization is needed by OnSerialize and by manual sending from authority
        // public only for tests
        public static void SerializeIntoWriter(NetworkWriter writer, Vector3 position, Quaternion rotation, Vector3 scale, bool compressRotation, bool syncScale, float sendTime)
        {
            // serialize position, rotation, scale
            // => compress rotation from 4*4=16 to 4 bytes
            // => less bandwidth = better CCU tests / scale
            writer.WriteVector3(position);
            if (compressRotation)
            {
                // smalles three compression for 3D
                writer.WriteUInt32(Compression.CompressQuaternion(rotation));
            }
            else
            {
                // uncompressed for 2D
                writer.WriteQuaternion(rotation);
            }
            writer.WriteSingle(sendTime);
            if (syncScale) { writer.WriteVector3(scale); }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            // use local position/rotation/scale for VR support
            SerializeIntoWriter(writer, targetComponent.localPosition, targetComponent.localRotation, targetComponent.localScale, compressRotation, syncScale, Time.time);
            return true;
        }

        // try to estimate movement speed for a data point based on how far it
        // moved since the previous one
        // => if this is the first time ever then we use our best guess:
        //    -> delta based on transform.localPosition
        //    -> elapsed based on send interval hoping that it roughly matches
        static float EstimateMovementSpeed(DataPoint from, DataPoint to, Transform transform, float sendInterval)
        {
            Vector3 delta = to.localPosition - (from != null ? from.localPosition : transform.localPosition);
            float elapsed = from != null ? to.senderTimeStamp - from.senderTimeStamp : sendInterval;
            // avoid NaN
            return elapsed > 0 ? delta.magnitude / elapsed : 0;
        }

        // serialization is needed by OnSerialize and by manual sending from authority
        void DeserializeFromReader(NetworkReader reader)
        {
            // put it into a data point immediately
            DataPoint temp = new DataPoint
            {
                // deserialize position, rotation, scale
                // (rotation is optionally compressed)
                localPosition = reader.ReadVector3(),
                localRotation = compressRotation
                                ? Compression.DecompressQuaternion(reader.ReadUInt32())
                                : reader.ReadQuaternion(),
                // use current target scale, so we can check boolean and reader later, to see if the data is actually sent.
                localScale = targetComponent.localScale,
                timeStamp = Time.time,
                senderTimeStamp = reader.ReadSingle()
            };

            if (temp.senderTimeStamp < previousSenderTimeStamp)
            {
                Debug.Log("abandoned out-of-order NetworkTransform update (senderTimeStamp on new update was older than previously sent time");
                return;
            }
            else
                previousSenderTimeStamp = temp.senderTimeStamp;


            if (syncScale)
            {
                // Reader length is checked here, 12 is used as thats the current Vector3 (3 floats) amount.
                // In rare cases people may do mis-matched builds, log useful warning message, and then do not process missing scale data.
                if (reader.Length >= 13) { temp.localScale = reader.ReadVector3(); }
                else { Debug.LogWarning("Reader length does not contain enough data for a scale, please check that both server and client builds syncScale booleans match.", this); }
            }

            // movement speed: based on how far it moved since last time
            // has to be calculated before 'start' is overwritten
            temp.movementSpeed = EstimateMovementSpeed(goal, temp, targetComponent, syncInterval);

            // reassign start wisely
            // -> first ever data point? then make something up for previous one
            //    so that we can start interpolation without waiting for next.
            if (start == null)
            {
                start = new DataPoint
                {
                    timeStamp = Time.time - (syncInterval/2),
                    // local position/rotation for VR support
                    localPosition = targetComponent.localPosition,
                    localRotation = targetComponent.localRotation,
                    localScale = targetComponent.localScale,
                    movementSpeed = temp.movementSpeed
                };
            }
            // -> second or nth data point? then update previous, but:
            //    we start at where ever we are right now, so that it's
            //    perfectly smooth and we don't jump anywhere
            //
            //    example if we are at 'x':
            //
            //        A--x->B
            //
            //    and then receive a new point C:
            //
            //        A--x--B
            //              |
            //              |
            //              C
            //
            //    then we don't want to just jump to B and start interpolation:
            //
            //              x
            //              |
            //              |
            //              C
            //
            //    we stay at 'x' and interpolate from there to C:
            //
            //           x..B
            //            \ .
            //             \.
            //              C
            //
            else
            {                
                float newDistance = Vector3.Distance(goal.localPosition, temp.localPosition);

                start = goal;

                // teleport / lag / obstacle detection: only continue at current
                // position if we aren't too far away
                //
                // local position/rotation for VR support
                if (Vector3.Distance(targetComponent.localPosition, temp.localPosition) < newDistance)
                {
                    start.localPosition = targetComponent.localPosition;
                    if (syncRotation) start.localRotation = targetComponent.localRotation;
                    if (syncScale) start.localScale = targetComponent.localScale;
                }
            }

            // set new destination in any case. new data is best data.
            goal = temp;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            // deserialize
            DeserializeFromReader(reader);
        }

        // local authority client sends sync message to server for broadcasting
        [Command(channel = Channels.Unreliable)]
        void CmdClientToServerSync(ArraySegment<byte> payload)
        {
            // Ignore messages from client if not in client authority mode
            if (!clientAuthority)
                return;

            // deserialize payload
            using (PooledNetworkReader networkReader = NetworkReaderPool.GetReader(payload))
                DeserializeFromReader(networkReader);

            // server-only mode does no interpolation to save computations,
            // but let's set the position directly
            if (isServer && !isClient)
                ApplyPositionRotationScale(goal.localPosition, goal.localRotation, goal.localScale);

            // set dirty so that OnSerialize broadcasts it
            SetDirtyBit(1UL);
        }

        // where are we in the timeline between start and goal? [0,1]
        static float CurrentInterpolationFactor(DataPoint start, DataPoint goal)
        {
            if (start != null)
            {
                float difference = goal.timeStamp - start.timeStamp;

                // the moment we get 'goal', 'start' is supposed to
                // start, so elapsed time is based on:
                float elapsed = Time.time - goal.timeStamp;
                // avoid NaN
                return difference > 0 ? elapsed / difference : 0;
            }
            return 0;
        }

        static Vector3 InterpolatePosition(DataPoint start, DataPoint goal, Vector3 currentPosition)
        {
            if (start != null)
            {
                // Option 1: simply interpolate based on time. but stutter
                // will happen, it's not that smooth. especially noticeable if
                // the camera automatically follows the player
                //   float t = CurrentInterpolationFactor();
                //   return Vector3.Lerp(start.position, goal.position, t);

                // Option 2: always += speed
                // -> speed is 0 if we just started after idle, so always use max
                //    for best results
                float speed = Mathf.Max(start.movementSpeed, goal.movementSpeed);
                return Vector3.MoveTowards(currentPosition, goal.localPosition, speed * Time.deltaTime);
            }
            return currentPosition;
        }

        static Quaternion InterpolateRotation(DataPoint start, DataPoint goal, Quaternion defaultRotation)
        {
            if (start != null)
            {
                float t = CurrentInterpolationFactor(start, goal);
                return Quaternion.Slerp(start.localRotation, goal.localRotation, t);
            }
            return defaultRotation;
        }

        Vector3 InterpolateScale(DataPoint start, DataPoint goal, Vector3 currentScale)
        {
            if (start != null && interpolateScale)
            {
                float t = CurrentInterpolationFactor(start, goal);
                return Vector3.Lerp(start.localScale, goal.localScale, t);
            }
            else
            {
                return currentScale;
            }
        }

        // teleport / lag / stuck detection
        // -> checking distance is not enough since there could be just a tiny
        //    fence between us and the goal
        // -> checking time always works, this way we just teleport if we still
        //    didn't reach the goal after too much time has elapsed
        bool NeedsTeleport()
        {
            // calculate time between the two data points
            float startTime = start != null ? start.timeStamp : Time.time - syncInterval;
            float goalTime = goal != null ? goal.timeStamp : Time.time;
            float difference = goalTime - startTime;
            float timeSinceGoalReceived = Time.time - goalTime;
            //increasing to teleport less and lerp more
            return timeSinceGoalReceived > difference * 10; //return timeSinceGoalReceived > difference * 5;
        }

        // moved since last time we checked it?
        bool HasEitherMovedRotatedScaled()
        {
            // moved or rotated or scaled?
            // local position/rotation/scale for VR support
            bool moved = Vector3.Distance(lastPosition, targetComponent.localPosition) > localPositionSensitivity;
            bool scaled = syncScale && Vector3.Distance(lastScale, targetComponent.localScale) > localScaleSensitivity;
            bool rotated = syncRotation && Quaternion.Angle(lastRotation, targetComponent.localRotation) > localRotationSensitivity;

            // save last for next frame to compare
            // (only if change was detected. otherwise slow moving objects might
            //  never sync because of C#'s float comparison tolerance. see also:
            //  https://github.com/vis2k/Mirror/pull/428)
            bool change = moved || rotated || scaled;
            if (change)
            {
                // local position/rotation for VR support
                lastPosition = targetComponent.localPosition;
                lastRotation = targetComponent.localRotation;
                lastScale = targetComponent.localScale;
            }
            return change;
        }

        // set position carefully depending on the target component
        void ApplyPositionRotationScale(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // local position/rotation for VR support
            targetComponent.localPosition = position;
            if (syncRotation) targetComponent.localRotation = rotation;
            if (syncScale) targetComponent.localScale = scale;
        }
        //experimental, applying speeds to local rigidbody from estimates based on movement updates... -RH
        void ApplyEstimatedVelocityToLocalRigidbodyIfPresent()
        {
            if (targetComponent.gameObject.GetComponent<Rigidbody>() != null)
            {
                
                targetComponent.gameObject.GetComponent<Rigidbody>().velocity = (goal.localPosition - start.localPosition).normalized * start.movementSpeed;

            }
        }

        void Update()
        {
            // if server then always sync to others.
            if (isServer)
            {
                // just use OnSerialize via SetDirtyBit only sync when position
                // changed. set dirty bits 0 or 1
                SetDirtyBit(HasEitherMovedRotatedScaled() ? 1UL : 0UL);
            }

            // no 'else if' since host mode would be both
            if (isClient)
            {
                // send to server if we have local authority (and aren't the server)
                // -> only if connectionToServer has been initialized yet too
                if (!isServer && IsClientWithAuthority)
                {
                    // check only each 'syncInterval'
                    if (Time.time - lastClientSendTime >= syncInterval)
                    {
                        if (HasEitherMovedRotatedScaled())
                        {
                            // serialize
                            // local position/rotation for VR support
                            using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
                            {
                                SerializeIntoWriter(writer, targetComponent.localPosition, targetComponent.localRotation, targetComponent.localScale, compressRotation, syncScale, Time.time);

                                // send to server
                                CmdClientToServerSync(writer.ToArraySegment());
                            }
                        }
                        lastClientSendTime = Time.time;
                    }
                }

                // apply interpolation on client for all players
                // unless this client has authority over the object. could be
                // himself or another object that he was assigned authority over
                if (!IsClientWithAuthority)
                {
                    // received one yet? (initialized?)
                    if (goal != null)
                    {
                        // teleport or interpolate
                        if (AlwaysTeleport || NeedsTeleport())
                        {
                            // local position/rotation for VR support
                            ApplyPositionRotationScale(goal.localPosition, goal.localRotation, goal.localScale);

                            // reset data points so we don't keep interpolating
                            start = null;
                            goal = null;
                        }
                        else
                        {
                            // local position/rotation for VR support
                            ApplyPositionRotationScale(InterpolatePosition(start, goal, targetComponent.localPosition),
                                                       InterpolateRotation(start, goal, targetComponent.localRotation),
                                                       InterpolateScale(start, goal, targetComponent.localScale));

                            if (applyExperimentalRigidbodyVelocity)
                                ApplyEstimatedVelocityToLocalRigidbodyIfPresent();
                        }
                    }
                }
            }
        }

        #region Server Teleport (force move player)
        /// <summary>
        /// Server side teleportation.
        /// This method will override this GameObject's current Transform.Position to the Vector3 you have provided
        /// and send it to all other Clients to override it at their side too.
        /// </summary>
        /// <param name="position">Where to teleport this GameObject</param>
        [Server]
        public void ServerTeleport(Vector3 position)
        {
            Quaternion rotation = transform.rotation;
            ServerTeleport(position, rotation);
        }

        /// <summary>
        /// Server side teleportation.
        /// This method will override this GameObject's current Transform.Position and Transform.Rotation
        /// to the Vector3 you have provided
        /// and send it to all other Clients to override it at their side too.
        /// </summary>
        /// <param name="position">Where to teleport this GameObject</param>
        /// <param name="rotation">Which rotation to set this GameObject</param>
        [Server]
        public void ServerTeleport(Vector3 position, Quaternion rotation)
        {
            // To prevent applying the position updates received from client (if they have ClientAuth) while being teleported.

            // clientAuthorityBeforeTeleport defaults to false when not teleporting, if it is true then it means that teleport was previously called but not finished
            // therefore we should keep it as true so that 2nd teleport call doesn't clear authority
            clientAuthorityBeforeTeleport = clientAuthority || clientAuthorityBeforeTeleport;
            clientAuthority = false;

            DoTeleport(position, rotation);

            // tell all clients about new values
            RpcTeleport(position, rotation, clientAuthorityBeforeTeleport);
        }

        void DoTeleport(Vector3 newPosition, Quaternion newRotation)
        {
            transform.position = newPosition;
            transform.rotation = newRotation;

            // Since we are overriding the position we don't need a goal and start.
            // Reset them to null for fresh start
            goal = null;
            start = null;
            lastPosition = newPosition;
            lastRotation = newRotation;
        }

        [ClientRpc]
        void RpcTeleport(Vector3 newPosition, Quaternion newRotation, bool isClientAuthority)
        {
            DoTeleport(newPosition, newRotation);

            // only send finished if is owner and is ClientAuthority on server
            if (hasAuthority && isClientAuthority)
                CmdTeleportFinished();
        }

        /// <summary>
        /// This RPC will be invoked on server after client finishes overriding the position.
        /// </summary>
        /// <param name="initialAuthority"></param>
        [Command]
        void CmdTeleportFinished()
        {
            if (clientAuthorityBeforeTeleport)
            {
                clientAuthority = true;

                // reset value so doesn't effect future calls, see note in ServerTeleport
                clientAuthorityBeforeTeleport = false;
            }
            else
            {
                Debug.LogWarning("Client called TeleportFinished when clientAuthority was false on server", this);
            }
        }
        #endregion

        static void DrawDataPointGizmo(DataPoint data, Color color)
        {
            // use a little offset because transform.localPosition might be in
            // the ground in many cases
            Vector3 offset = Vector3.up * 0.01f;

            // draw position
            Gizmos.color = color;
            Gizmos.DrawSphere(data.localPosition + offset, 0.5f);

            // draw forward and up
            // like unity move tool
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(data.localPosition + offset, data.localRotation * Vector3.forward);

            // like unity move tool
            Gizmos.color = Color.green;
            Gizmos.DrawRay(data.localPosition + offset, data.localRotation * Vector3.up);
        }

        static void DrawLineBetweenDataPoints(DataPoint data1, DataPoint data2, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(data1.localPosition, data2.localPosition);
        }

        // draw the data points for easier debugging
        void OnDrawGizmos()
        {
            // draw start and goal points
            if (start != null) DrawDataPointGizmo(start, Color.gray);
            if (goal != null) DrawDataPointGizmo(goal, Color.white);

            // draw line between them
            if (start != null && goal != null) DrawLineBetweenDataPoints(start, goal, Color.cyan);
        }
    }
}
