using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using Ascendant.Physics;
using Ascendant.Graphics.lighting;
using OpenTK;
using System.Diagnostics;

namespace Ascendant.Graphics.objects {
    class World {
        readonly protected List<MovableObject> children = new List<MovableObject>();
        readonly protected Physics.Simulation sim;
        readonly internal protected Lighting lights;

        readonly Stopwatch timer = Stopwatch.StartNew();

        readonly protected internal Game parentGame;

        protected internal int modelToCameraMatrixUnif;
        protected internal int normalModelToCameraMatrixUnif;

        public World(Game game) {
            this.parentGame = game;
            sim = new Simulation(children);
            lights = new Lighting(children);
            this.parentGame.window.FocusedChanged += (o, e) => {
                if (this.parentGame.window.Focused)
                    timer.Start();
                else
                    timer.Stop();
            };
        }

        public void Render() {
            lights.Render(parentGame.Camera.GetWorldToCameraMatrix());
            foreach (MovableObject obj in children) {
                obj.Render();
            }
        }


        internal void InitializeOpenGL(int program) {
            modelToCameraMatrixUnif = GL.GetUniformLocation(parentGame.gameProgram, "modelToCameraMatrix");
            normalModelToCameraMatrixUnif = GL.GetUniformLocation(parentGame.gameProgram, "normalModelToCameraMatrix");
            lights.InitializeOpenGL(program);
            foreach (MovableObject obj in children) {
                obj.InitializeOpenGL(program);
            }
            MaterialLoader.LoadMaterialBufferBlock(program);
        }

        internal void Pause() {
            timer.Stop();
        }
        internal void Resume() {
            timer.Start();
        }
        internal void Update() {
            if (timer.IsRunning) {
                sim.RunSim(timer.ElapsedMilliseconds);
            }
        }

        internal void addRootObjects(List<MovableObject> newObjects) {
            this.children.AddRange(newObjects);
        }

        internal void setLighting(Vector4 ambient, Vector4 background) {
            lights.SetGlobalLighting(ambient, background);
        }
    }
}
