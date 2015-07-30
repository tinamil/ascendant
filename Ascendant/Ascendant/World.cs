using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using Ascendant.Physics;
using Ascendant.Graphics.lighting;
using OpenTK;
using System.Diagnostics;
using BulletSharp;
using Ascendant;

namespace Ascendant.Graphics.objects {
    /**
     * Constructor
     * AddObjects
     * SetSun
     * InitializeOpenGL
     * while(!done){
     *    Update
     *    Render
     * }
     * */
    class World {
        private DisplayWorld displayWorld;
        private PhysicsWorld pWorld;

        public World(Game game) {
            this.displayWorld = new DisplayWorld(game);
            this.pWorld = new PhysicsWorld(game);
        }

        public void Render() {
            displayWorld.Render(pWorld.ElapsedTime);
        }

        public void InitializeOpenGL(int program) {
            displayWorld.initializeOpenGL(program);
        }

        internal void Update() {
            displayWorld.Update(pWorld.ElapsedTime);
            pWorld.Update();
        }

        public void addObject(DisplayObject obj) {
            pWorld.addObject(obj);
            displayWorld.add(obj);
        }

        internal void setSun(TimedLinearInterpolator<Sun> sunTimer) {
            displayWorld.lights.setSun(sunTimer);
        }

        internal Vector4 getBackgroundColor() {
            return displayWorld.lights.sunTimer.Interpolate(Sun.Alpha(pWorld.ElapsedTime)).background;
        }
    }
}
