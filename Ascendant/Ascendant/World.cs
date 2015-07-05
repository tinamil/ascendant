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
        readonly protected List<GameObject> children = new List<GameObject>();
        protected Physics.Simulation sim;
        internal protected Lighting lights;

        readonly Stopwatch timer = Stopwatch.StartNew();

        readonly protected internal Game parentGame;

        protected internal int modelToCameraMatrixUnif;
        protected internal int normalModelToCameraMatrixUnif;
        private bool g_useGammaDisplay = true;

        public World(Game game) {
            this.parentGame = game;
            this.parentGame.window.FocusedChanged += (o, e) => {
                if (this.parentGame.window.Focused)
                    timer.Start();
                else
                    timer.Stop();
            };
            game.window.KeyPress += (o, e) => {
                if (e.KeyChar == 'g') {
                    g_useGammaDisplay = !g_useGammaDisplay;
                }
            };
        }

        public void Render() {
            if (g_useGammaDisplay)
                GL.Enable(EnableCap.FramebufferSrgb);
            else
                GL.Disable(EnableCap.FramebufferSrgb);


            lights.Render(parentGame.Camera.GetWorldToCameraMatrix());
            foreach (GameObject obj in children) {
                obj.Render();
            }

        }


        internal void InitializeOpenGL(int program) {
            modelToCameraMatrixUnif = GL.GetUniformLocation(parentGame.gameProgram, "modelToCameraMatrix");
            normalModelToCameraMatrixUnif = GL.GetUniformLocation(parentGame.gameProgram, "normalModelToCameraMatrix");
            lights.InitializeOpenGL(program);
            foreach (GameObject obj in children) {
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

        internal void addRootObjects(List<GameObject> newObjects) {
            this.children.AddRange(newObjects);

            sim = new Simulation(children);
            lights = new Lighting(children);
        }

        internal void setLighting(Vector4 ambient, Vector4 background) {
            lights.SetGlobalLighting(ambient, background);
        }
    }
}
