﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using OpenTK;
using Ascendant.Graphics;
using OpenTK.Graphics.OpenGL4;
using BulletSharp;

namespace Ascendant.Physics {

    class Simulation {

        readonly protected List<GameObject> objects;

        DiscreteDynamicsWorld dynamicsWorld;
        public Simulation(List<GameObject> worldObjects) {
            this.objects = worldObjects;


            BroadphaseInterface broadphase = new BulletSharp.DbvtBroadphase();

            // Set up the collision configuration and dispatcher
            CollisionConfiguration collisionConfiguration = new DefaultCollisionConfiguration();
            CollisionDispatcher dispatcher = new CollisionDispatcher(collisionConfiguration);

            // The actual physics solver
            ConstraintSolver solver = new BulletSharp.NncgConstraintSolver();

            // The world.
            dynamicsWorld = new BulletSharp.SoftBody.SoftRigidDynamicsWorld(dispatcher, broadphase, solver, collisionConfiguration);
            //dynamicsWorld.Gravity = new Vector3(0, 0f, 0);

            foreach (GameObject obj in objects) {
                addObjToPhysics(dynamicsWorld, obj);
            }
            dynamicsWorld.SolverInfo.RestingContactRestitutionThreshold = 5;
            dynamicsWorld.SolverInfo.SplitImpulse = 1;
            dynamicsWorld.SolverInfo.SplitImpulsePenetrationThreshold = -0.02f;
            dynamicsWorld.SolverInfo.NumIterations = 20;
        }

        private void addObjToPhysics(DiscreteDynamicsWorld dynamicsWorld, GameObject obj) {
            dynamicsWorld.AddRigidBody(obj.body);
            if (obj.constraint != null) dynamicsWorld.AddConstraint(obj.constraint);
            foreach (GameObject child in obj.children) {
                addObjToPhysics(dynamicsWorld, child);
            }
        }

        const float dt = 0.005f;
        float prevTime = 0;
        public void RunSim(long elapsedMillis) {

            float newTime = elapsedMillis / 1000.0f;
            float frameTime = newTime - prevTime;
            if (frameTime > .250) frameTime = .250f;
            if (frameTime < 0) return;
            prevTime = newTime;
            dynamicsWorld.StepSimulation(frameTime, 10, dt);

            //      int numManifolds = dynamicsWorld.Dispatcher.NumManifolds;
            //      for (int i=0;i<numManifolds;i++) {
            //  PersistentManifold contactManifold =  dynamicsWorld.Dispatcher.GetManifoldByIndexInternal(i);
            //CollisionObject obA = contactManifold.Body0;
            //  CollisionObject obB = contactManifold.Body1;
            //      }
        }
    }
}
